"""Pieces of paper.json shared by every paper reader, and what a reader reports back."""

from dataclasses import dataclass, field

from .model import Crop
from .parts import PartSource
from .text import escape


@dataclass(frozen=True)
class PartResult:
    part: dict
    crops: list[Crop] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)
    checks: list[str] = field(default_factory=list)


@dataclass(frozen=True)
class Conversion:
    paper_parts: dict[int, dict]
    crops: list[Crop]
    warnings: list[str]
    checks: list[str]


def make_group(interaction: str, source: PartSource, questions: list[dict], instructions: str | None = None, **extra) -> dict:
    group: dict = {"interactionType": interaction}
    instructions = instructions if instructions is not None else source.instructions
    if instructions:
        group["instructions"] = instructions
    group.update(extra)
    group["questions"] = questions
    return group


def key_answers(part_no: int, number: int, key: dict[int, list[str]], options: list[dict] | None) -> tuple[list[str], list[str]]:
    answers = key.get(number, [])
    if not answers:
        return [], [f"Part {part_no}, question {number}: no answer in the answer key."]
    if options is not None:
        keys = [option["key"] for option in options]
        if len(answers) != 1 or answers[0] not in keys:
            return answers, [f"Part {part_no}, question {number}: answer {' / '.join(answers)} is not one of the options ({', '.join(keys)})."]
    return answers, []


def choice_question(part_no: int, number: int, prompt: str, options: list[dict], key: dict[int, list[str]],
                    stimulus: str | None = None) -> tuple[dict, list[str]]:
    question: dict = {"number": number}
    if prompt:
        question["promptHtml"] = escape(prompt)
    if stimulus:
        question["stimulusHtml"] = stimulus
    question["options"] = options
    question["answers"], warnings = key_answers(part_no, number, key, options)
    if len(options) < 2:
        warnings.append(f"Part {part_no}, question {number}: found {len(options)} option(s); expected A, B and C.")
    return question, warnings


def merge(results: dict[int, PartResult], extra_warnings: list[str]) -> Conversion:
    return Conversion(
        {part_no: result.part for part_no, result in results.items()},
        [crop for result in results.values() for crop in result.crops],
        [warning for result in results.values() for warning in result.warnings] + extra_warnings,
        [check for result in results.values() for check in result.checks],
    )
