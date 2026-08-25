export interface Lead {
  id: number
  firstName: string
  lastName: string
  email: string
  phone: string | null
  company: string | null
  jobTitle: string | null
  linkedInUrl: string | null
  source: string
  externalId: string | null
  status: number
  createdAt: string
  updatedAt: string | null
}

export const LeadStatus = {
  New: 0,
  Contacted: 1,
  Replied: 2,
  Qualified: 3,
  Unresponsive: 4,
  OptedOut: 5,
} as const

export const leadStatusLabels: Record<number, string> = {
  [LeadStatus.New]: 'New',
  [LeadStatus.Contacted]: 'Contacted',
  [LeadStatus.Replied]: 'Replied',
  [LeadStatus.Qualified]: 'Qualified',
  [LeadStatus.Unresponsive]: 'Unresponsive',
  [LeadStatus.OptedOut]: 'Opted out',
}

export interface Campaign {
  id: number
  name: string
  description: string | null
  status: number
  subjectTemplate?: string | null
  bodyTemplate?: string | null
  createdAt: string
  startedAt: string | null
  completedAt: string | null
  outreachMessages?: OutreachMessage[]
}

export const campaignStatusLabels: Record<number, string> = {
  0: 'Draft',
  1: 'Active',
  2: 'Paused',
  3: 'Completed',
  4: 'Cancelled',
}

export interface OutreachMessage {
  id: number
  leadId: number
  lead?: Lead
  campaignId: number
  channel: number
  subject: string | null
  body: string
  status: number
  errorMessage: string | null
  createdAt: string
  sentAt: string | null
}

export const OutreachMessageStatus = {
  Draft: 0,
  Queued: 1,
  Sent: 2,
  Failed: 3,
  Bounced: 4,
} as const

export const messageStatusLabels: Record<number, string> = {
  0: 'Draft',
  1: 'Queued',
  2: 'Sent',
  3: 'Failed',
  4: 'Bounced',
}

export type Scope = 'any' | 'current' | 'past'

export type ExpertiseMode =
  | 'must_have_any'
  | 'must_have_all'
  | 'must_not_have_any'
  | 'must_not_have_all'

export interface ProfileSearchRequest {
  keywords?: string[]
  jobTitles?: string[]
  jobTitleScope?: Scope
  companies?: string[]
  companyScope?: Scope
  expertises?: string[]
  expertiseMode?: ExpertiseMode
  countries?: string[]
  states?: string[]
  cities?: string[]
  pageNumber?: number
  pageSize?: number
  maxResults?: number
}

export interface RhetorikProfileResult {
  position: number
  profile_data?: {
    profile_id: string
    profile_first_name: string
    profile_last_name: string
    profile_headline?: string | null
    profile_summary?: string | null
    profile_expertises?: string[] | null
    profile_tags?: string[] | null
    profile_address?: {
      country?: string | null
      state?: string | null
      city?: string | null
    } | null
  } | null
  contact_data?: {
    contact_current_experiences?:
      | { company_name?: string | null; job_title?: string | null; current?: boolean }[]
      | null
  } | null
}

export interface ProfileSearchResponse {
  counts?: { profiles_total_results?: number }
  results: RhetorikProfileResult[]
  pagination?: { current: number; last_page: number; next_page: number | null }
}

export interface AutocompleteSuggestion {
  content: string
  count?: number | null
}

