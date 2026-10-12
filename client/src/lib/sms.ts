// Keeps SMS bodies to plain ASCII so Twilio always sends GSM-7 (160-character) segments instead
// of 70-character UCS-2 segments. Mirrors the backend Gsm7 sanitizer.
const ASCII_ALIASES: Record<string, string> = {
  '\u2018': "'",
  '\u2019': "'",
  '\u201A': "'",
  '\u201B': "'",
  '\u201C': '"',
  '\u201D': '"',
  '\u201E': '"',
  '\u201F': '"',
  '\u2010': '-',
  '\u2011': '-',
  '\u2012': '-',
  '\u2013': '-',
  '\u2014': '-',
  '\u2015': '-',
  '\u2026': '...',
  '\u2025': '...',
  '\u22EF': '...',
  '\u2022': '-',
  '\u00B7': '-',
  '\u2023': '-',
  '\u2043': '-',
  '\u25AA': '-',
  '\u2190': '<-',
  '\u2192': '->',
  '\u2191': '|',
  '\u2193': '|',
  '\u00A9': '(c)',
  '\u00AE': '(r)',
  '\u2122': '(tm)',
  '\u2020': '*',
  '\u2021': '*',
  '\u2032': "'",
  '\u2033': '"',
  '\u00A0': ' ',
  '\u2007': ' ',
}

export function smsToAscii(text: string): string {
  return Array.from(text.normalize('NFKD'))
    .map((ch) => {
      const code = ch.codePointAt(0)!
      if (code === 10 || code === 13 || code === 9) return ch
      if (code >= 0x20 && code <= 0x7e) return ch
      if (code >= 0x0300 && code <= 0x036f) return '' // combining accents (NFKD decomposed)
      return ASCII_ALIASES[ch] ?? ''
    })
    .join('')
}

// Twilio billing: a single GSM-7 segment is 160 characters; 2+ segments carry 153 each.
export function smsSegments(text: string): { chars: number; segments: number } {
  const chars = smsToAscii(text).length
  if (chars === 0) return { chars: 0, segments: 0 }
  const segments = chars <= 160 ? 1 : Math.ceil(chars / 153)
  return { chars, segments }
}