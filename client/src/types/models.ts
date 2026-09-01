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
  campaigns?: CampaignRef[] | null
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

export interface PaginatedLeads {
  items: Lead[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface Campaign {
  id: number
  name: string
  description: string | null
  status: number
  channel: number
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

export const OutreachChannel = {
  Email: 0,
  Sms: 1,
  WhatsApp: 2,
  LinkedIn: 3,
} as const

export const outreachChannelLabels: Record<number, string> = {
  [OutreachChannel.Email]: 'Email',
  [OutreachChannel.Sms]: 'SMS',
  [OutreachChannel.WhatsApp]: 'WhatsApp',
  [OutreachChannel.LinkedIn]: 'LinkedIn InMail',
}

export interface LinkedInStatus {
  signedIn: boolean
  dryRun: boolean
  userDataDir: string
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
  profileIds?: string[]
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
  jobTitleSuggestions?: string[]
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
      | { company_name?: string | null; raw_company_name?: string | null; job_title?: string | null; current?: boolean }[]
      | null
  } | null
}

export interface CampaignRef {
  id: number
  name: string
}

export interface EnrichedRhetorikProfileResult extends RhetorikProfileResult {
  lead_id?: number | null
  campaigns?: CampaignRef[] | null
}

export interface ProfileSearchResponse {
  counts?: { profiles_total_results?: number }
  results: RhetorikProfileResult[]
  pagination?: { current: number; last_page: number; next_page: number | null }
}

export interface EnrichedProfileSearchResponse {
  counts?: { profiles_total_results?: number }
  results: EnrichedRhetorikProfileResult[]
  pagination?: { current: number; last_page: number; next_page: number | null }
}

export interface AutocompleteSuggestion {
  content: string
  count?: number | null
}

export interface ScottyAttachment {
  url: string | null
  media_type?: string | null
  caption?: string | null
}

export interface ScottyMetadata {
  platform_session_id?: string | null
  continuity_key?: string | null
  agent_instance_id?: string | null
  agent_definition_id?: string | null
  routing_rule_id?: string | null
  pipeline_definition_id?: string | null
  pipeline_instance_id?: string | null
}

export interface ScottyChatResponse {
  output: string | null
  attachments: ScottyAttachment[]
  metadata: ScottyMetadata | null
}

export interface ScottyCallResponse {
  url: string | null
  token: string | null
  metadata: ScottyMetadata | null
}



