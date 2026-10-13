/** A stretch of a page's text, by character offsets, so it can be found again after the page is drawn again. */
export interface TextSpan {
  /** Which page of the paper (part and question numbers). */
  root: string
  start: number
  end: number
}

/** Answer controls change as the candidate answers; their text never counts towards offsets. */
const SKIPPED = '[data-gap], button, input, textarea, select, [data-no-annotate]'

/** The page's text nodes in reading order, without the answer controls. */
export function textNodes(root: Element): Text[] {
  const walker = root.ownerDocument.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
    acceptNode: (node) => (node.parentElement?.closest(SKIPPED) ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT),
  })
  const nodes: Text[] = []
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    nodes.push(node as Text)
  }

  return nodes
}

/** The selection as offsets in the page's text; null when it is empty or not on this page. */
export function spanFromRange(root: Element, rootKey: string, range: Range): TextSpan | null {
  if (range.collapsed) {
    return null
  }

  let offset = 0
  let start: number | null = null
  let end: number | null = null
  for (const node of textNodes(root)) {
    if (range.intersectsNode(node)) {
      const from = node === range.startContainer ? range.startOffset : 0
      const to = node === range.endContainer ? range.endOffset : node.length
      if (to > from) {
        start ??= offset + from
        end = offset + to
      }
    }

    offset += node.length
  }

  return start === null || end === null || end <= start ? null : { root: rootKey, start, end }
}

/** A live range for a stored span on the page now shown; null if the page's text no longer has it. */
export function rangeFromSpan(root: Element, span: TextSpan): Range | null {
  const range = root.ownerDocument.createRange()
  let offset = 0
  let hasStart = false
  for (const node of textNodes(root)) {
    const next = offset + node.length
    if (!hasStart && span.start >= offset && span.start < next) {
      range.setStart(node, span.start - offset)
      hasStart = true
    }

    if (hasStart && span.end > offset && span.end <= next) {
      range.setEnd(node, span.end - offset)
      return range
    }

    offset = next
  }

  return null
}
