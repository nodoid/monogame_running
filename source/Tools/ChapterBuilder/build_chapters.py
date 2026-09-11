#!/usr/bin/env python3
"""
Builds the per-chapter source folders (source/Chapter01 ... Chapter42) from the master
template in source/Tools/Template.

The template is the finished game. Code that belongs to a particular chapter is marked with
conditional-compilation symbols, one per chapter, meaning "this chapter or later":

    #if CH14                 // present from Chapter 14 onwards
    #if !CH14                // present only before Chapter 14
    #if CH05 && !CH07        // present in Chapters 5 and 6
    #if CH32 || CH38         // present from Chapter 32, or from Chapter 38

The template defines every CHnn symbol, so it builds as the finished game. For each chapter
this script evaluates the CHnn conditions, keeps or drops the lines they guard, and removes the
directives themselves, leaving ordinary code. Conditions using any other symbols (DEBUG,
ANDROID, ...) are left alone. XML files use the same directives inside comments
(<!-- #if CH41 -->), and the MonoGame content file (.mgcb) uses them as comment lines.

A whole C# file can be limited to a range of chapters with a first line such as
    // @since 16            or    // @since 04 @until 33
Other files (images, sounds, projects) are included according to FILE_RULES below; content
files are included when the chapter's .mgcb file builds them.

Usage:
    python3 build_chapters.py            # build every chapter
    python3 build_chapters.py 12 13      # build only these chapters
"""

import json
import os
import re
import shutil
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parent.parent
SOURCE = TOOLS.parent
TEMPLATE = TOOLS / "Template"
CHAPTERS = json.loads((TOOLS / "ChapterBuilder" / "chapters.json").read_text(encoding="utf-8"))["chapters"]
LAST_CHAPTER = len(CHAPTERS)

TEXT_EXTENSIONS = {".cs", ".csproj", ".props", ".targets", ".slnx", ".xml", ".plist", ".storyboard", ".mgcb",
                   ".fx", ".json", ".txt", ".spritefont", ".md", ".sh"}
SKIP_DIRECTORIES = {"bin", "obj", ".vs", ".idea"}
SKIP_FILES = {".DS_Store"}

# (path prefix relative to the template, first chapter, last chapter or None)
FILE_RULES = [
    ("MonsterMaze.Tests/", 41, None),
    ("global.json", 41, None),
    ("Store/", 42, None),
    ("MonsterMaze.Core/Content/Fonts/", 2, None),
    ("MonsterMaze.Core/Content/Effects/PostProcess.fx", 29, None),
    ("MonsterMaze.Core/Content/Effects/Compiled/Android/PostProcess.xnb", 29, None),
    ("MonsterMaze.Core/Content/Effects/Compiled/iOS/PostProcess.xnb", 29, None),
    ("MonsterMaze.Core/Content/Effects/Bloom.fx", 31, None),
    ("MonsterMaze.Core/Content/Effects/Compiled/Android/Bloom.xnb", 31, None),
    ("MonsterMaze.Core/Content/Effects/Compiled/iOS/Bloom.xnb", 31, None),
]

CONTENT_DIRECTORY = "MonsterMaze.Core/Content/"
CONTENT_FILE = CONTENT_DIRECTORY + "MonsterMaze.mgcb"

DIRECTIVE = re.compile(r"^\s*(?:<!--\s*)?#(if|elif|else|endif)\b\s*(.*?)\s*(?:-->)?\s*$")
CH_TERM = re.compile(r"^!?CH(\d\d)$")
SINCE = re.compile(r"^\s*//\s*@since\s+(\d+)(?:\s+@until\s+(\d+))?\s*$")


def is_chapter_condition(expression):
    terms = [term.strip() for part in expression.split("||") for term in part.split("&&")]
    return bool(terms) and all(CH_TERM.match(term) for term in terms)


def evaluate(expression, chapter):
    def term_value(term):
        term = term.strip()
        wanted = chapter >= int(CH_TERM.match(term).group(1))
        return not wanted if term.startswith("!") else wanted

    return any(all(term_value(term) for term in part.split("&&")) for part in expression.split("||"))


def process_text(text, chapter, path):
    """Keeps the lines that belong to this chapter and removes the chapter directives."""
    output = []
    # Each frame: [is_chapter_condition, parent_active, branch_taken, active]
    stack = []
    active = True

    for number, line in enumerate(text.split("\n"), start=1):
        match = DIRECTIVE.match(line)
        if match:
            keyword, expression = match.group(1), match.group(2)
            if keyword == "if":
                if is_chapter_condition(expression):
                    value = active and evaluate(expression, chapter)
                    stack.append([True, active, value, value])
                    active = value
                    continue
                stack.append([False, active, True, active])
                if active:
                    output.append(line)
                continue

            if not stack:
                raise ValueError(f"{path}:{number}: #{keyword} without #if")
            frame = stack[-1]
            if keyword == "endif":
                stack.pop()
                if not frame[0] and frame[1]:
                    output.append(line)
                active = frame[1]
                continue
            if keyword == "elif":
                if frame[0]:
                    if not is_chapter_condition(expression):
                        raise ValueError(f"{path}:{number}: mixed #elif in a chapter condition")
                    value = frame[1] and not frame[2] and evaluate(expression, chapter)
                    frame[2] = frame[2] or value
                    frame[3] = value
                    active = value
                    continue
                if frame[1]:
                    output.append(line)
                continue
            # else
            if frame[0]:
                value = frame[1] and not frame[2]
                frame[2] = True
                frame[3] = value
                active = value
                continue
            if frame[1]:
                output.append(line)
            continue

        if active:
            output.append(line)

    if stack:
        raise ValueError(f"{path}: unterminated #if")
    return "\n".join(output)


def tidy_csharp(text):
    """Removes the blank lines that dropped blocks can leave behind."""
    lines = text.split("\n")
    result = []
    for line in lines:
        blank = line.strip() == ""
        if blank and result and (result[-1].strip() == "" or result[-1].strip() == "{"):
            continue
        if line.strip().startswith("}") and result and result[-1].strip() == "":
            result.pop()
        result.append(line)
    while result and result[-1].strip() == "":
        result.pop()
    return "\n".join(result) + "\n"


def rule_allows(relative, chapter):
    for prefix, first, last in FILE_RULES:
        if relative == prefix or relative.startswith(prefix):
            return chapter >= first and (last is None or chapter <= last)
    return True


def chapter_folder(chapter):
    return SOURCE / f"Chapter{chapter:02d}"


def build_chapter(chapter):
    destination = chapter_folder(chapter)
    outputs = {}

    # Work out which content files this chapter's content project builds.
    content_text = process_text((TEMPLATE / CONTENT_FILE).read_text(encoding="utf-8"), chapter, CONTENT_FILE)
    built_content = {CONTENT_DIRECTORY + line.split(":", 1)[1].strip()
                     for line in content_text.split("\n") if line.startswith("/build:")}

    for root, directories, files in os.walk(TEMPLATE):
        directories[:] = [d for d in directories if d not in SKIP_DIRECTORIES]
        for name in files:
            if name in SKIP_FILES:
                continue
            source = Path(root) / name
            relative = source.relative_to(TEMPLATE).as_posix()

            if relative == "MonsterMaze.slnx":
                target_relative = f"Chapter{chapter:02d}.slnx"
            else:
                target_relative = relative

            if not rule_allows(relative, chapter):
                continue

            # Content assets (other than the content project itself) are copied only if built.
            is_content_asset = relative.startswith(CONTENT_DIRECTORY) and relative != CONTENT_FILE \
                and not relative.startswith(CONTENT_DIRECTORY + "Fonts/") \
                and not relative.startswith(CONTENT_DIRECTORY + "Effects/")
            if is_content_asset and relative not in built_content:
                continue

            target = destination / target_relative
            if source.suffix.lower() in TEXT_EXTENSIONS:
                text = source.read_text(encoding="utf-8")
                if source.suffix == ".cs":
                    first_line = text.split("\n", 1)[0]
                    since = SINCE.match(first_line)
                    if since:
                        first = int(since.group(1))
                        last = int(since.group(2)) if since.group(2) else None
                        if chapter < first or (last is not None and chapter > last):
                            continue
                        text = text.split("\n", 1)[1]
                text = process_text(text, chapter, relative)
                if relative == "Directory.Build.props":
                    text = "\n".join(line for line in text.split("\n") if "@template-only" not in line)
                    text = re.sub(r"<ChapterNumber>\d+</ChapterNumber>",
                                  f"<ChapterNumber>{chapter:02d}</ChapterNumber>", text)
                if source.suffix == ".cs":
                    text = tidy_csharp(text)
                if re.search(r"#(if|elif)\s+!?CH\d\d", text):
                    raise ValueError(f"{relative}: chapter directive left behind")
                outputs[target_relative] = text.encode("utf-8")
            else:
                outputs[target_relative] = source.read_bytes()

    missing = [path for path in built_content if path not in outputs]
    if missing:
        raise ValueError(f"Chapter {chapter}: content file(s) not found: {missing}")

    sync_folder(destination, outputs)

    write_readme(chapter)


def sync_folder(destination, outputs):
    """
    Writes the chapter's files, touching only those that changed and deleting any that are no
    longer part of it. Build output (bin and obj) is left alone, so rebuilds stay incremental.
    """
    existing = list_files(destination, include_all=True) if destination.exists() else {}
    for relative, data in outputs.items():
        if existing.get(relative) != data:
            target = destination / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
    for relative in existing:
        if relative not in outputs and relative != "README.md":
            (destination / relative).unlink()

    # Remove folders left empty.
    for root, directories, files in os.walk(destination, topdown=False):
        path = Path(root)
        if path != destination and not any(path.iterdir()) and path.name not in SKIP_DIRECTORIES:
            path.rmdir()


def list_files(folder, include_all=False):
    files = {}
    for root, directories, names in os.walk(folder):
        directories[:] = [d for d in directories if d not in SKIP_DIRECTORIES]
        for name in names:
            if name in SKIP_FILES or (not include_all and (name == "README.md" or name.endswith(".slnx"))):
                continue
            path = Path(root) / name
            files[path.relative_to(folder).as_posix()] = path.read_bytes()
    return files


def write_readme(chapter):
    info = CHAPTERS[chapter - 1]
    folder = chapter_folder(chapter)
    lines = [
        f"# Chapter {chapter}: {info['title']}",
        "",
        f"*{info['part']}*",
        "",
        "## What this chapter covers",
        "",
    ]
    lines += [f"- {topic}" for topic in info["topics"]]

    if chapter > 1 and chapter_folder(chapter - 1).exists():
        before = list_files(chapter_folder(chapter - 1))
        after = list_files(folder)
        added = sorted(path for path in after if path not in before)
        changed = sorted(path for path in after if path in before and before[path] != after[path])
        removed = sorted(path for path in before if path not in after)
        lines += ["", f"## Changes since Chapter {chapter - 1}", ""]
        if not (added or changed or removed):
            lines.append("No code changes: this chapter is about tools, testing or process rather than code.")
        for title, paths in (("New files", added), ("Changed files", changed), ("Removed files", removed)):
            if paths:
                lines += [f"**{title}**", ""] + [f"- `{path}`" for path in paths] + [""]
    else:
        lines += [""]

    lines += [
        "",
        "## Building and running",
        "",
        f"Open `Chapter{chapter:02d}.slnx` in Rider or Visual Studio, or build from the command line:",
        "",
        "```",
        "dotnet build MonsterMaze.Android/MonsterMaze.Android.csproj",
        "dotnet build MonsterMaze.iOS/MonsterMaze.iOS.csproj -r iossimulator-arm64",
        "```",
        "",
        "To run on a connected Android device or emulator:",
        "",
        "```",
        "dotnet build MonsterMaze.Android/MonsterMaze.Android.csproj -t:Run",
        "```",
        "",
        "To run in the iOS Simulator:",
        "",
        "```",
        "dotnet build MonsterMaze.iOS/MonsterMaze.iOS.csproj -t:Run -r iossimulator-arm64",
        "```",
        "",
    ]
    if chapter >= 41:
        lines += ["To run the unit tests:", "", "```", "dotnet test --project MonsterMaze.Tests", "```", ""]
    (folder / "README.md").write_text("\n".join(lines), encoding="utf-8")


def write_master_solution():
    lines = ["<Solution>"]
    for info in CHAPTERS:
        number = info["number"]
        folder = f"Chapter{number:02d}"
        if not chapter_folder(number).exists():
            continue
        lines.append(f'  <Folder Name="/Chapter {number:02d} - {info["title"]}/">')
        for project in ("MonsterMaze.Core/MonsterMaze.Core.csproj",
                        "MonsterMaze.Android/MonsterMaze.Android.csproj",
                        "MonsterMaze.iOS/MonsterMaze.iOS.csproj",
                        "MonsterMaze.Tests/MonsterMaze.Tests.csproj"):
            if (chapter_folder(number) / project).exists():
                lines.append(f'    <Project Path="{folder}/{project}" />')
        lines.append("  </Folder>")
    lines.append("</Solution>")
    (SOURCE / "MonsterMaze.slnx").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main():
    chapters = [int(argument) for argument in sys.argv[1:]] or list(range(1, LAST_CHAPTER + 1))
    for chapter in chapters:
        build_chapter(chapter)
        print(f"Chapter {chapter:02d}: {CHAPTERS[chapter - 1]['title']}")
    write_master_solution()
    print("Master solution: source/MonsterMaze.slnx")


if __name__ == "__main__":
    main()
