import type { PaperOption } from './api'

const GAP = /\{\{q:(\d+)\}\}/g

/** In the review page a typed or chosen gap shows as its question number: 'Mrs {{q:6}}' → 'Mrs (6)'. */
export function gapsAsLabels(html: string): string {
  return html.replace(GAP, (_, number: string) => `<span class="st-gap">(${number})</span>`)
}

export function isAccepted(option: PaperOption, answers: string[]): boolean {
  return answers.includes(option.key)
}
