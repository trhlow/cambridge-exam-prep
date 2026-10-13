import type { Contrast, TextSize } from '../api/types'

export interface DisplayOptions {
  textSize: TextSize
  contrast: Contrast
}

type Props = DisplayOptions & {
  isTimerHidden: boolean
  onChange: (options: DisplayOptions) => void
  onToggleTimer: () => void
  onSubmissionPage: () => void
  onClose: () => void
}

const CONTRASTS: { value: Contrast; label: string }[] = [
  { value: 'BlackOnWhite', label: 'Black on white' },
  { value: 'WhiteOnBlack', label: 'White on black' },
  { value: 'YellowOnBlack', label: 'Yellow on black' },
]

const SIZES: { value: TextSize; label: string }[] = [
  { value: 'Regular', label: 'Regular' },
  { value: 'Large', label: 'Large' },
  { value: 'ExtraLarge', label: 'Extra large' },
]

/**
 * VE-18: Inspera's Options screen (☰). It covers the whole test, so it is also how a candidate hides their screen
 * when leaving the desk. Contrast and text size apply at once; the timer can be hidden.
 */
export function OptionsScreen({ textSize, contrast, isTimerHidden, onChange, onToggleTimer, onSubmissionPage, onClose }: Props) {
  return (
    <div className="opt-screen" role="dialog" aria-modal="true" aria-label="Options">
      <div className="opt-screen-head">
        <h2>Options</h2>
        <button type="button" className="opt-screen-close" aria-label="Close options" onClick={onClose}>
          ✕
        </button>
      </div>
      <div className="opt-screen-body">
        <button type="button" className="btn btn--primary opt-screen-submit" onClick={onSubmissionPage}>
          Go to submission page
        </button>

        <fieldset className="opt-screen-group">
          <legend>Contrast</legend>
          <div className="opt-screen-choices">
            {CONTRASTS.map((choice) => (
              <label key={choice.value} className={`opt-screen-choice contrast-sample contrast-sample--${choice.value}`}>
                <input type="radio" name="contrast" checked={contrast === choice.value} onChange={() => onChange({ textSize, contrast: choice.value })} />
                {choice.label}
              </label>
            ))}
          </div>
        </fieldset>

        <fieldset className="opt-screen-group">
          <legend>Text size</legend>
          <div className="opt-screen-choices">
            {SIZES.map((choice) => (
              <label key={choice.value} className={`opt-screen-choice size-sample size-sample--${choice.value}`}>
                <input type="radio" name="text-size" checked={textSize === choice.value} onChange={() => onChange({ textSize: choice.value, contrast })} />
                {choice.label}
              </label>
            ))}
          </div>
        </fieldset>

        <div className="opt-screen-group">
          <button type="button" className="btn btn--secondary" onClick={onToggleTimer}>
            {isTimerHidden ? 'Show remaining time' : 'Hide remaining time'}
          </button>
        </div>
      </div>
    </div>
  )
}
