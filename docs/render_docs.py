#!/usr/bin/env python3
"""Render the vehicle physics markdown guide to a full standalone HTML page."""

from __future__ import annotations

import argparse
import datetime as _dt
import html
from pathlib import Path
import sys

import markdown


DEFAULT_INPUT = "vehicle-physics-and-creation-guide.md"
DEFAULT_OUTPUT = "vehicle-physics-and-creation-guide.html"


def build_html(title: str, body_html: str, source_name: str) -> str:
    now_utc = _dt.datetime.now(_dt.timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    safe_title = html.escape(title)
    safe_source = html.escape(source_name)
    return f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{safe_title}</title>
  <style>
    :root {{
      color-scheme: light dark;
      --bg: #0e1117;
      --panel: #161b22;
      --text: #e6edf3;
      --muted: #9da7b3;
      --link: #58a6ff;
      --border: #30363d;
      --code: #11161d;
      --mono: Consolas, "Cascadia Mono", "Courier New", monospace;
      --sans: "Segoe UI", "Noto Sans", Tahoma, sans-serif;
    }}
    @media (prefers-color-scheme: light) {{
      :root {{
        --bg: #f6f8fa;
        --panel: #ffffff;
        --text: #1f2328;
        --muted: #59636e;
        --link: #0969da;
        --border: #d0d7de;
        --code: #f6f8fa;
      }}
    }}
    html, body {{ margin: 0; padding: 0; }}
    body {{
      background: var(--bg);
      color: var(--text);
      font-family: var(--sans);
      line-height: 1.55;
      font-size: 16px;
    }}
    .page {{
      max-width: 1100px;
      margin: 24px auto;
      padding: 0 18px 28px;
    }}
    .doc {{
      background: var(--panel);
      border: 1px solid var(--border);
      border-radius: 12px;
      padding: 22px;
      box-sizing: border-box;
      overflow-wrap: anywhere;
    }}
    h1, h2, h3, h4 {{ line-height: 1.25; }}
    h1 {{ margin-top: 0; }}
    a {{
      color: var(--link);
      text-decoration: none;
    }}
    a:hover {{ text-decoration: underline; }}
    code {{
      font-family: var(--mono);
      font-size: 0.95em;
      background: var(--code);
      border: 1px solid var(--border);
      border-radius: 6px;
      padding: 0.1em 0.35em;
    }}
    pre {{
      background: var(--code);
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 12px;
      overflow: auto;
    }}
    pre code {{
      border: 0;
      background: transparent;
      padding: 0;
    }}
    table {{
      border-collapse: collapse;
      width: 100%;
      margin: 12px 0;
      font-size: 0.95em;
    }}
    th, td {{
      border: 1px solid var(--border);
      padding: 8px 10px;
      vertical-align: top;
      text-align: left;
    }}
    th {{
      background: color-mix(in srgb, var(--panel) 75%, var(--border));
    }}
    hr {{
      border: 0;
      border-top: 1px solid var(--border);
      margin: 20px 0;
    }}
    blockquote {{
      border-left: 4px solid var(--border);
      margin: 12px 0;
      padding: 4px 12px;
      color: var(--muted);
    }}
    .footer {{
      margin-top: 14px;
      color: var(--muted);
      font-size: 0.9em;
    }}
  </style>
</head>
<body>
  <main class="page">
    <article class="doc">
{body_html}
      <div class="footer">Rendered from {safe_source} on {now_utc}</div>
    </article>
  </main>
</body>
</html>
"""


def normalize_toc_block(text: str) -> str:
    """Convert plain link lines under '## Table of Contents' into markdown list items."""
    lines = text.splitlines()
    if not lines:
        return text

    out: list[str] = []
    i = 0
    while i < len(lines):
        line = lines[i]
        out.append(line)

        if line.strip() != "## Table of Contents":
            i += 1
            continue

        i += 1
        while i < len(lines) and lines[i].strip() == "":
            out.append(lines[i])
            i += 1

        toc_lines: list[str] = []
        while i < len(lines) and lines[i].strip() != "":
            toc_lines.append(lines[i].strip())
            i += 1

        looks_like_toc = bool(toc_lines) and all(
            item.startswith("[") and "](" in item and item.endswith(")")
            for item in toc_lines
        )

        if looks_like_toc:
            out.extend(f"- {item}" for item in toc_lines)
        else:
            out.extend(toc_lines)

    result = "\n".join(out)
    if text.endswith("\n"):
        result += "\n"
    return result


def render_markdown(input_path: Path, output_path: Path) -> None:
    text = input_path.read_text(encoding="utf-8-sig")
    text = normalize_toc_block(text)
    body = markdown.markdown(
        text,
        extensions=[
            "extra",
            "tables",
            "fenced_code",
            "sane_lists",
            "toc",
            "attr_list",
            "md_in_html",
        ],
        output_format="html5",
    )
    title = input_path.stem.replace("-", " ").title()
    full_html = build_html(title=title, body_html=body, source_name=input_path.name)
    output_path.write_text(full_html, encoding="utf-8")


def parse_args() -> argparse.Namespace:
    script_dir = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(
        description="Render markdown into a standalone HTML page."
    )
    parser.add_argument(
        "input",
        nargs="?",
        default=str(script_dir / DEFAULT_INPUT),
        help="Input markdown path.",
    )
    parser.add_argument(
        "output",
        nargs="?",
        default=str(script_dir / DEFAULT_OUTPUT),
        help="Output html path.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()

    if not input_path.exists():
        print(f"Input file not found: {input_path}", file=sys.stderr)
        return 1

    output_path.parent.mkdir(parents=True, exist_ok=True)
    render_markdown(input_path, output_path)
    print(f"Rendered HTML: {output_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
