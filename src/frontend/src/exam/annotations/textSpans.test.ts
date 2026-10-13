import { rangeFromSpan, spanFromRange } from './textSpans'

function page(html: string): HTMLElement {
  const root = document.createElement('div')
  root.innerHTML = html
  document.body.replaceChildren(root)
  return root
}

const textOf = (range: Range | null) => range?.toString()

describe('text spans (VE-19)', () => {
  it('turns a selection across elements into character offsets and back', () => {
    const root = page('<p>Hello <b>big</b> world</p><p>Second line</p>')
    const range = document.createRange()
    range.setStart(root.querySelector('p')!.firstChild!, 3) // "lo big wor"
    range.setEnd(root.querySelector('p')!.lastChild!, 4)

    const span = spanFromRange(root, 'p1', range)

    expect(span).toEqual({ root: 'p1', start: 3, end: 13 })
    expect(textOf(rangeFromSpan(root, span!))).toBe('lo big wor')
  })

  it('ignores text inside answer controls, so typing an answer does not move highlights', () => {
    const root = page('<p>Before <span data-gap="19"><button>difference</button></span> after</p>')
    const after = root.querySelector('p')!.lastChild!
    const range = document.createRange()
    range.setStart(after, 1)
    range.setEnd(after, 6)

    const span = spanFromRange(root, 'p4', range)
    root.querySelector('button')!.textContent = 'a much longer answer'

    expect(span).toEqual({ root: 'p4', start: 8, end: 13 })
    expect(textOf(rangeFromSpan(root, span!))).toBe('after')
  })

  it('has no span for an empty selection or one outside the page, and no range for a span past the end', () => {
    const root = page('<p>Short</p>')
    const outside = document.createElement('p')
    outside.textContent = 'elsewhere'
    document.body.append(outside)
    const collapsed = document.createRange()
    collapsed.setStart(root.querySelector('p')!.firstChild!, 2)
    const elsewhere = document.createRange()
    elsewhere.selectNodeContents(outside)

    expect(spanFromRange(root, 'p1', collapsed)).toBeNull()
    expect(spanFromRange(root, 'p1', elsewhere)).toBeNull()
    expect(rangeFromSpan(root, { root: 'p1', start: 2, end: 40 })).toBeNull()
  })
})
