import { gapsAsLabels, isAccepted } from './paperPreview'

describe('gapsAsLabels', () => {
  it('shows each answer gap as its question number', () => {
    expect(gapsAsLabels('<p>Mrs {{q:6}} and {{q:12}} a.m.</p>')).toBe('<p>Mrs <span class="st-gap">(6)</span> and <span class="st-gap">(12)</span> a.m.</p>')
  })

  it('leaves text without gaps alone', () => {
    expect(gapsAsLabels('<p>{{ not a gap }}</p>')).toBe('<p>{{ not a gap }}</p>')
  })
})

describe('isAccepted', () => {
  it('marks the option that is the answer', () => {
    expect(isAccepted({ key: 'B', text: null, imageUrl: null }, ['B'])).toBe(true)
    expect(isAccepted({ key: 'A', text: null, imageUrl: null }, ['B'])).toBe(false)
  })
})
