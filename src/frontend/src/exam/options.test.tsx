import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { OptionsScreen } from './OptionsScreen'

const setup = (overrides: Partial<Parameters<typeof OptionsScreen>[0]> = {}) => {
  const props = {
    textSize: 'Regular' as const,
    contrast: 'BlackOnWhite' as const,
    isTimerHidden: false,
    onChange: vi.fn(),
    onToggleTimer: vi.fn(),
    onSubmissionPage: vi.fn(),
    onClose: vi.fn(),
    ...overrides,
  }
  render(<OptionsScreen {...props} />)
  return props
}

describe('OptionsScreen (VE-18)', () => {
  it('offers the three contrasts and three text sizes of Inspera, with the current ones selected', () => {
    setup({ contrast: 'YellowOnBlack', textSize: 'Large' })

    expect(screen.getByRole('dialog', { name: 'Options' })).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: 'Black on white' })).not.toBeChecked()
    expect(screen.getByRole('radio', { name: 'White on black' })).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: 'Yellow on black' })).toBeChecked()
    expect(screen.getByRole('radio', { name: 'Regular' })).not.toBeChecked()
    expect(screen.getByRole('radio', { name: 'Large' })).toBeChecked()
    expect(screen.getByRole('radio', { name: 'Extra large' })).toBeInTheDocument()
  })

  it('reports a new choice, the timer switch, the submission page and closing', async () => {
    const props = setup()

    await userEvent.click(screen.getByRole('radio', { name: 'White on black' }))
    await userEvent.click(screen.getByRole('radio', { name: 'Extra large' }))
    await userEvent.click(screen.getByRole('button', { name: 'Hide remaining time' }))
    await userEvent.click(screen.getByRole('button', { name: 'Go to submission page' }))
    await userEvent.click(screen.getByRole('button', { name: 'Close options' }))

    expect(props.onChange).toHaveBeenCalledWith({ textSize: 'Regular', contrast: 'WhiteOnBlack' })
    expect(props.onChange).toHaveBeenCalledWith({ textSize: 'ExtraLarge', contrast: 'BlackOnWhite' })
    expect(props.onToggleTimer).toHaveBeenCalled()
    expect(props.onSubmissionPage).toHaveBeenCalled()
    expect(props.onClose).toHaveBeenCalled()
  })

  it('says Show when the timer is hidden', () => {
    setup({ isTimerHidden: true })

    expect(screen.getByRole('button', { name: 'Show remaining time' })).toBeInTheDocument()
  })
})
