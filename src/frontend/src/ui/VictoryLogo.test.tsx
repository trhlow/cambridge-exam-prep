import { render, screen } from '@testing-library/react'
import { VictoryLogo } from './VictoryLogo'

describe('VictoryLogo (VE-24)', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('shows the logo the centre uploaded', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({ centreName: 'Victory English Centre', logoUrl: '/api/site/logo?v=9', updatedAt: '2026-10-18T00:00:00Z' }), { status: 200 })))

    render(<VictoryLogo />)

    const logo = await screen.findByRole('img', { name: 'Victory English Centre' })
    expect(logo).toHaveAttribute('src', '/api/site/logo?v=9')
  })
})
