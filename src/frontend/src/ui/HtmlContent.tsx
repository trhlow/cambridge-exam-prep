import DOMPurify from 'dompurify'

type Props = {
  html: string | null
  className?: string
}

/**
 * Exam text from the API. The server already strips scripts when a paper is imported (HtmlSanitizer);
 * DOMPurify here is a second line of defence at the point where HTML is injected.
 */
export function HtmlContent({ html, className }: Props) {
  if (!html) {
    return null
  }

  return <div className={className} dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(html) }} />
}
