export interface Lead {
  id: number
  firstName: string
  lastName: string
  email: string
  phone: string | null
  company: string | null
  jobTitle: string | null
  location: string | null
  linkedInUrl: string | null
  source: string
  externalId: string | null
  status: number
  preferredChannel: number | null
  country: string | null
  createdAt: string
  updatedAt: string | null
  campaigns?: CampaignRef[] | null
}

export interface LeadEmail {
  id: number
  email: string
  type: string
  isVerified: boolean
  isPrimary: boolean
  priority: number
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
  sequenceId: number | null
  jobId: number | null
  subjectTemplate?: string | null
  bodyTemplate?: string | null
  isRunning: boolean
  lastRunAt: string | null
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
  [OutreachChannel.LinkedIn]: 'LinkedIn InMail',
}

export interface LinkedInStatus {
  signedIn: boolean
  dryRun: boolean
  userDataDir: string
  available?: boolean
  mode?: string
  error?: string | null
}

export interface AuthUser {
  id: number
  email: string
  displayName: string
  role: string
  sendFromAddress: string | null
  sendFromName: string | null
  replyToAddress: string | null
}

export interface AuthResponse {
  token: string
  user: AuthUser
}

export interface AuthStatus {
  hasUsers: boolean
}

export interface MicrosoftStatus {
  connected: boolean
  accountEmail: string | null
  connectedAt: string | null
  configured: boolean
}

export interface AppUser {
  id: number
  email: string
  displayName: string
  role: string
  isActive: boolean
  createdAt: string
  lastLoginAt: string | null
  microsoftConnected: boolean
}

export interface Interview {
  id: number
  leadId: number
  candidateName: string | null
  campaignId: number | null
  jobId: number | null
  startAt: string
  durationMinutes: number
  status: string
  teamsJoinUrl: string | null
  summary: string | null
  attended: boolean | null
  hasTranscript: boolean
}

export interface LeadNote {
  id: number
  role: string
  content: string
  isEscalation: boolean
  createdAt: string
}

export interface InterviewSlot {
  startUtc: string
  label: string
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
  stepOrder: number | null
  openedAt: string | null
  clickedAt: string | null
  repliedAt: string | null
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
    profile_languages?: string[] | null
    profile_address?: {
      country?: string | null
      state?: string | null
      city?: string | null
    } | null
    profile_social_links?: { name?: string | null; url?: string | null }[] | null
  } | null
  contact_data?: {
    contact_current_experiences?:
      | { company_name?: string | null; raw_company_name?: string | null; job_title?: string | null; current?: boolean; start_date?: string | null; end_date?: string | null }[]
      | null
    contact_emails?: { email?: string | null; priority?: number | null }[] | null
    contact_phones?: { phone?: string | null; type?: string | null; priority?: number | null }[] | null
  } | null
  resume_data?: {
    experiences?:
      | { company_name?: string | null; raw_company_name?: string | null; job_title?: string | null; current?: boolean; start_date?: string | null; end_date?: string | null }[]
      | null
    educations?:
      | { educational_establishment?: string | null; diploma?: string | null; specialization?: string | null; start_date?: string | null; end_date?: string | null }[]
      | null
    certifications?:
      | { name?: string | null; description?: string | null; authority?: string | null; date?: string | null }[]
      | null
    memberships?:
      | { title?: string | null; description?: string | null; name?: string | null; start_date?: string | null; end_date?: string | null }[]
      | null
    publications?:
      | { name?: string | null; description?: string | null; date?: string | null }[]
      | null
    patents?:
      | { name?: string | null; number?: string | null; date?: string | null }[]
      | null
    awards?:
      | { name?: string | null; description?: string | null; date?: string | null }[]
      | null
  } | null
  lead_id?: number | null
  campaigns?: CampaignRef[] | null
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

export const JobType = {
  FullTime: 0,
  PartTime: 1,
  FixedTerm: 2,
  Casual: 3,
  Internship: 4,
} as const

export const jobTypeLabels: Record<number, string> = {
  [JobType.FullTime]: 'Full-time',
  [JobType.PartTime]: 'Part-time',
  [JobType.FixedTerm]: 'Fixed-term',
  [JobType.Casual]: 'Casual',
  [JobType.Internship]: 'Internship',
}

export const Flexibility = {
  OnSite: 0,
  Remote: 1,
  Hybrid: 2,
} as const

export const flexibilityLabels: Record<number, string> = {
  [Flexibility.OnSite]: 'On-site',
  [Flexibility.Remote]: 'Remote',
  [Flexibility.Hybrid]: 'Hybrid',
}

export const SalaryType = {
  Annual: 0,
  Hourly: 1,
  Fixed: 2,
  NotDisclosed: 3,
} as const

export const salaryTypeLabels: Record<number, string> = {
  [SalaryType.Annual]: 'Annual',
  [SalaryType.Hourly]: 'Hourly',
  [SalaryType.Fixed]: 'Fixed',
  [SalaryType.NotDisclosed]: 'Not disclosed',
}

export const JobStatus = {
  Draft: 0,
  Active: 1,
  Closed: 2,
} as const

export const jobStatusLabels: Record<number, string> = {
  [JobStatus.Draft]: 'Draft',
  [JobStatus.Active]: 'Active',
  [JobStatus.Closed]: 'Closed',
}

export interface Job {
  id: number
  title: string
  location: string | null
  industry: string | null
  type: number
  flexibility: number
  salaryType: number
  salaryFrom: number | null
  salaryTo: number | null
  salaryNotes: string | null
  startDate: string | null
  expiryDate: string | null
  hiringManager: string | null
  department: string | null
  advertUrl: string | null
  advertCopy: string | null
  mustHaves: string | null
  niceToHaves: string | null
  education: string | null
  skills: string | null
  attractiveReasons: string | null
  screeningDetails: string | null
  status: number
  createdAt: string
  updatedAt: string | null
}

export interface JobInput {
  title: string
  location?: string | null
  industry?: string | null
  type?: number
  flexibility?: number
  salaryType?: number
  salaryFrom?: number | null
  salaryTo?: number | null
  salaryNotes?: string | null
  startDate?: string | null
  expiryDate?: string | null
  hiringManager?: string | null
  department?: string | null
  advertUrl?: string | null
  advertCopy?: string | null
  mustHaves?: string | null
  niceToHaves?: string | null
  education?: string | null
  skills?: string | null
  attractiveReasons?: string | null
  screeningDetails?: string | null
  status?: number
}

export interface OrganizationProfile {
  id: number
  orgName: string | null
  about: string | null
  evp: string | null
  culture: string | null
  hiringProcess: string | null
  eeo: string | null
  guardRails: string | null
  createdAt: string
  updatedAt: string | null
}

export interface PolicyGuardrails {
  id: number
  whatAiMayAnswer: string | null
  escalationTriggers: string | null
  refusalTopics: string | null
  requiredDisclaimers: string | null
  marketRules: string | null
  needsHumanStates: string | null
  confidenceThreshold: number | null
  createdAt: string
  updatedAt: string | null
}

export interface GenerateContentRequest {
  jobId: number
  stepName: string
  channel: string
  stepNumber: number
  goal?: string
  additionalInstructions?: string
}

export interface GeneratedContent {
  subject: string
  body: string
}

export const ConsentStatus = {
  Unknown: 0,
  OptedIn: 1,
  OptedOut: 2,
  DoNotContact: 3,
} as const

export const consentStatusLabels: Record<number, string> = {
  [ConsentStatus.Unknown]: 'Unknown',
  [ConsentStatus.OptedIn]: 'Opted in',
  [ConsentStatus.OptedOut]: 'Opted out',
  [ConsentStatus.DoNotContact]: 'Do not contact',
}

export const ConsentChannel = {
  Email: 0,
  Sms: 1,
  WhatsApp: 2,
  LinkedIn: 3,
} as const

export const consentChannelLabels: Record<number, string> = {
  [ConsentChannel.Email]: 'Email',
  [ConsentChannel.Sms]: 'SMS',
  [ConsentChannel.LinkedIn]: 'LinkedIn',
}

export interface ChannelConsent {
  channel: number
  status: number
  optInSource: string | null
  optInDate: string | null
  optOutDate: string | null
  notes: string | null
}

export interface LeadConsentResponse {
  leadId: number
  preferredChannel: number | null
  country: string | null
  consents: ChannelConsent[]
}

export interface LeadProfile {
  id: number
  leadId: number
  headline: string | null
  summary: string | null
  selfReportedSkills: string | null
  aiInferredSkills: string | null
  workExperience: string | null
  education: string | null
  certifications: string | null
  industries: string | null
  languages: string | null
  memberships: string | null
  publications: string | null
  awards: string | null
  patents: string | null
  lastUpdatedByCandidate: string | null
}

export const SequenceStatus = {
  Draft: 0,
  Active: 1,
  Paused: 2,
  Completed: 3,
} as const

export const sequenceStatusLabels: Record<number, string> = {
  [SequenceStatus.Draft]: 'Draft',
  [SequenceStatus.Active]: 'Active',
  [SequenceStatus.Paused]: 'Paused',
  [SequenceStatus.Completed]: 'Completed',
}

export const SequenceStepCondition = {
  Always: 0,
  IfNotOpened: 1,
  IfNotClicked: 2,
  IfNotReplied: 3,
} as const

export const sequenceStepConditionLabels: Record<number, string> = {
  [SequenceStepCondition.Always]: 'Send regardless of the previous step',
  [SequenceStepCondition.IfNotOpened]: 'If they have not opened the previous email',
  [SequenceStepCondition.IfNotClicked]: 'If they have not clicked a link in the previous email',
  [SequenceStepCondition.IfNotReplied]: 'If they have not replied',
}

export interface SequenceStep {
  id: number
  order: number
  name: string
  channel: number
  subjectTemplate: string | null
  bodyTemplate: string
  delayDays: number
  condition: number
}

export interface Sequence {
  id: number
  name: string
  description: string | null
  status: number
  sendDaysMask: number
  sendWindowStart: string
  sendWindowEnd: string
  includeUnsubscribe: boolean
  createdAt: string
  updatedAt: string | null
  steps: SequenceStep[]
}

export interface SequenceStepInput {
  id: number
  name: string
  channel: number
  subjectTemplate: string
  bodyTemplate: string
  delayDays: number
  condition: number
}

export interface SequenceInput {
  name: string
  description?: string
  status: number
  sendDaysMask: number
  sendWindowStart: string
  sendWindowEnd: string
  includeUnsubscribe: boolean
  steps: SequenceStepInput[]
}

export interface PersonalisationOption {
  key: string
  name: string
  description: string
  template: string
}

export interface SequencePreview {
  subject: string
  body: string
}

export interface ReportStep {
  stepNumber: number
  stepName: string
  channel: string
  sent: number
  delivered: number
  opened: number
  clicked: number
  replied: number
  failed: number
  bounced: number
}

export interface CampaignReportSummary {
  campaignId: number
  campaignName: string
  sequenceName: string | null
  totalCandidates: number
  steps: ReportStep[]
}

export interface CandidateReportRow {
  leadId: number
  candidateName: string
  email: string
  phone: string | null
  campaignName: string
  sequenceName: string | null
  addedBy: string | null
  addedAt: string | null
  currentStage: string
  daysInStage: number
  engagementStatus: string
  lastContact: string | null
  lastReply: string | null
  nextAction: string
  nextActionAt: string | null
  emailStatus: string
  smsStatus: string
  optOut: boolean
  goalAchieved: boolean
  humanAttention: boolean
}

export interface CandidateReportResponse {
  campaignId: number
  campaignName: string
  candidates: CandidateReportRow[]
}

export const weekDays = [
  { bit: 1, label: 'Sun' },
  { bit: 2, label: 'Mon' },
  { bit: 4, label: 'Tue' },
  { bit: 8, label: 'Wed' },
  { bit: 16, label: 'Thu' },
  { bit: 32, label: 'Fri' },
  { bit: 64, label: 'Sat' },
] as const



