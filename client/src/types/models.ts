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

export type LeadStatusValue = (typeof LeadStatus)[keyof typeof LeadStatus]

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
  createdAt: string
  startedAt: string | null
  completedAt: string | null
  outreachMessages?: OutreachMessage[]
}

export const CampaignStatus = {
  Draft: 0,
  Active: 1,
  Paused: 2,
  Completed: 3,
  Cancelled: 4,
} as const

export type CampaignStatusValue = (typeof CampaignStatus)[keyof typeof CampaignStatus]

export const campaignStatusLabels: Record<number, string> = {
  [CampaignStatus.Draft]: 'Draft',
  [CampaignStatus.Active]: 'Active',
  [CampaignStatus.Paused]: 'Paused',
  [CampaignStatus.Completed]: 'Completed',
  [CampaignStatus.Cancelled]: 'Cancelled',
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

export type OutreachMessageStatusValue =
  (typeof OutreachMessageStatus)[keyof typeof OutreachMessageStatus]

export const messageStatusLabels: Record<number, string> = {
  [OutreachMessageStatus.Draft]: 'Draft',
  [OutreachMessageStatus.Queued]: 'Queued',
  [OutreachMessageStatus.Sent]: 'Sent',
  [OutreachMessageStatus.Failed]: 'Failed',
  [OutreachMessageStatus.Bounced]: 'Bounced',
}

export interface RhetorikSearchRequest {
  keywords?: string[]
  jobTitles?: string[]
  companies?: string[]
  countries?: string[]
  pageNumber: number
  pageSize: number
}

export interface RhetorikContactResult {
  position: number
  contactData?: {
    contactId: string
    firstName: string
    lastName: string
    emails?: { address: string; type?: string }[] | null
    phones?: { number: string; type?: string }[] | null
    companyName?: string | null
    jobTitle?: string | null
    country?: string | null
  } | null
}

export interface RhetorikSearchResponse {
  counts?: { contactsTotalResults: number }
  results: RhetorikContactResult[]
  pagination?: { current: number; lastPage: number; nextPage: number | null }
}
