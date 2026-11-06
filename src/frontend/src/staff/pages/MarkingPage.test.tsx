import { render, screen } from '@testing-library/react'
import { adminApi, type AttemptMarking } from '../api'
import { MarkingPage } from './MarkingPage'

const marking = (passageHtml: string): AttemptMarking => ({
  attemptId: 7,
  sectionTitle: 'Reading & Writing',
  gradingStatus: 'NeedsManualMarking',
  rawScore: null,
  maxScore: 30,
  code: 'K7P2QX',
  questions: [
    {
      number: 31,
      partNo: 6,
      interactionType: 'Essay',
      response: 'Hi Jo, let us go on Saturday.',
      acceptedAnswers: [],
      isCorrect: null,
      score: null,
      points: 15,
      comment: null,
      promptHtml: '<p>Write an email to Jo.</p>',
      stimulusHtml: '<p>Say which film you want to see.</p>',
      passageHtml,
    },
  ],
})

describe('MarkingPage shows the task (A5)', () => {
  afterEach(() => vi.restoreAllMocks())

  it('shows the passage, the prompt and the stimulus above the answer, without scripts', async () => {
    vi.spyOn(adminApi, 'marking').mockResolvedValue(marking('<p>Part passage</p><img src="x" onerror="window.__pwned = true"><script>window.__pwned = true</script>'))

    const { container } = render(<MarkingPage attemptId={7} />)

    expect(await screen.findByText('Part passage')).toBeInTheDocument()
    expect(screen.getByText('Write an email to Jo.')).toBeInTheDocument()
    expect(screen.getByText('Say which film you want to see.')).toBeInTheDocument()
    expect(container.querySelector('script')).toBeNull()
    expect(container.querySelector('img')?.getAttribute('onerror')).toBeNull()
  })
})
