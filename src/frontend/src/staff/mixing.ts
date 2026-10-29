/**
 * VE-70 (Victory 29/10): the limits the server applies when papers are mixed (PaperMixPlan): 2–6 source papers make
 * between 4 and three times as many papers, or fewer when the section has so few parts that there are not that many
 * different ways to take them.
 */
export const MIN_SOURCES = 2
export const MAX_SOURCES = 6
export const MIN_VERSIONS = 4

export function maxVersions(sources: number, parts: number): number {
  return Math.min(3 * sources, sources ** parts)
}

/** The letter the patterns use for the n-th source (0 = A). */
export function sourceLetter(index: number): string {
  return String.fromCharCode(65 + index)
}
