import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type { BaseQueryFn, FetchArgs, FetchBaseQueryError } from '@reduxjs/toolkit/query'
import { AUTH_TOKEN_KEY } from '@/store/authSlice'
import type {
  AppUser,
  AuthResponse,
  AuthStatus,
  AuthUser,
  AutocompleteSuggestion,
  Campaign,
  CampaignReportSummary,
  CandidateReportResponse,
  EnrichedProfileSearchResponse,
  GeneratedContent,
  GenerateContentRequest,
  Interview,
  InterviewSlot,
  Job,
  JobInput,
  Lead,
  LeadConsentResponse,
  LeadNote,
  LeadProfile,
  LinkedInStatus,
  MicrosoftStatus,
  OrganizationProfile,
  OutreachMessage,
  PaginatedLeads,
  PersonalisationOption,
  PolicyGuardrails,
  ProfileSearchRequest,
  ScottyCallResponse,
  ScottyChatResponse,
  Sequence,
  SequenceInput,
  SequencePreview,
} from '@/types/models'

const rawBaseQuery = fetchBaseQuery({
  baseUrl: '/api',
  prepareHeaders: (headers) => {
    const token = localStorage.getItem(AUTH_TOKEN_KEY)
    if (token) {
      headers.set('Authorization', `Bearer ${token}`)
    }
    const apiKey = import.meta.env.VITE_API_KEY as string | undefined
    if (apiKey) {
      headers.set('X-API-Key', apiKey)
    }
    return headers
  },
})

const baseQueryWithReauth: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions,
) => {
  const result = await rawBaseQuery(args, api, extraOptions)
  if (result.error?.status === 401) {
    api.dispatch({ type: 'auth/clearCredentials' })
  }
  return result
}

export const apiSlice = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['Lead', 'Campaign', 'OutreachMessage', 'Sequence', 'Job', 'Organization', 'Policy', 'User', 'Interview'],
  endpoints: (builder) => ({
    getAuthStatus: builder.query<AuthStatus, void>({
      query: () => '/auth/status',
    }),
    setupAdmin: builder.mutation<AuthResponse, { email: string; displayName: string; password: string }>({
      query: (body) => ({ url: '/auth/setup', method: 'POST', body }),
    }),
    login: builder.mutation<AuthResponse, { email: string; password: string }>({
      query: (body) => ({ url: '/auth/login', method: 'POST', body }),
    }),
    getMe: builder.query<AuthUser, void>({
      query: () => '/auth/me',
    }),
    updateMe: builder.mutation<
      AuthUser,
      { displayName?: string; sendFromAddress?: string | null; sendFromName?: string | null; replyToAddress?: string | null }
    >({
      query: (body) => ({ url: '/auth/me', method: 'PUT', body }),
    }),
    logout: builder.mutation<{ signedOut: boolean }, void>({
      query: () => ({ url: '/auth/logout', method: 'POST' }),
    }),
    sendTestEmail: builder.mutation<{ sent: boolean; to: string; error?: string }, { to?: string }>({
      query: (body) => ({ url: '/email/test', method: 'POST', body }),
    }),
    getMicrosoftStatus: builder.query<MicrosoftStatus, void>({
      query: () => '/auth/microsoft/status',
    }),
    getMicrosoftConnectUrl: builder.query<{ url: string }, void>({
      query: () => '/auth/microsoft/connect-url',
    }),
    disconnectMicrosoft: builder.mutation<{ connected: boolean }, void>({
      query: () => ({ url: '/auth/microsoft/disconnect', method: 'POST' }),
    }),
    createExtensionCode: builder.mutation<{ code: string; expiresInMinutes: number }, void>({
      query: () => ({ url: '/auth/extension/code', method: 'POST' }),
    }),
    getUsers: builder.query<AppUser[], void>({
      query: () => '/users',
      providesTags: ['User'],
    }),
    createUser: builder.mutation<AppUser, { email: string; displayName: string; password: string; role?: string }>({
      query: (body) => ({ url: '/users', method: 'POST', body }),
      invalidatesTags: ['User'],
    }),
    changePassword: builder.mutation<{ changed: boolean }, { currentPassword: string; newPassword: string }>({
      query: (body) => ({ url: '/users/me/password', method: 'POST', body }),
    }),
    getUpcomingInterviews: builder.query<Interview[], void>({
      query: () => '/interviews',
      providesTags: ['Interview'],
    }),
    getLeadInterviews: builder.query<Interview[], number>({
      query: (leadId) => `/leads/${leadId}/interviews`,
      providesTags: ['Interview'],
    }),
    getInterviewSlots: builder.query<InterviewSlot[], number>({
      query: (leadId) => `/leads/${leadId}/interview-slots`,
    }),
    bookInterview: builder.mutation<
      Interview,
      { leadId: number; startAt: string; campaignId?: number | null; jobId?: number | null; panelUserIds?: number[] }
    >({
      query: ({ leadId, ...body }) => ({ url: `/leads/${leadId}/interviews`, method: 'POST', body }),
      invalidatesTags: ['Interview'],
    }),
    rescheduleInterview: builder.mutation<Interview, { id: number; startAt: string }>({
      query: ({ id, startAt }) => ({ url: `/interviews/${id}`, method: 'PUT', body: { startAt } }),
      invalidatesTags: ['Interview'],
    }),
    cancelInterview: builder.mutation<{ cancelled: boolean }, { id: number; reason?: string }>({
      query: ({ id, reason }) => ({ url: `/interviews/${id}/cancel`, method: 'POST', body: { reason } }),
      invalidatesTags: ['Interview'],
    }),
    processInterviewTranscript: builder.mutation<{ processed: boolean }, number>({
      query: (id) => ({ url: `/interviews/${id}/process-transcript`, method: 'POST' }),
      invalidatesTags: ['Interview', 'Lead'],
    }),
    updateInterviewRole: builder.mutation<{ role: number | null }, { id: number; jobId: number | null }>({
      query: ({ id, jobId }) => ({ url: `/interviews/${id}/role`, method: 'PUT', body: { jobId } }),
      invalidatesTags: ['Interview'],
    }),
    getLeadNotes: builder.query<LeadNote[], number>({
      query: (leadId) => `/leads/${leadId}/notes`,
      providesTags: ['Lead'],
    }),
    getLeads: builder.query<
      PaginatedLeads,
      {
        page: number
        pageSize: number
        campaignId?: number
        sortBy?: string
        sortOrder?: 'asc' | 'desc'
        addedFrom?: string
        addedTo?: string
      } | void
    >({
      query: (args) => {
        const page = args?.page ?? 1
        const pageSize = args?.pageSize ?? 100
        const params = new URLSearchParams()
        params.set('page', String(page))
        params.set('pageSize', String(pageSize))
        if (args?.campaignId) params.set('campaignId', String(args.campaignId))
        if (args?.sortBy) params.set('sortBy', args.sortBy)
        if (args?.sortOrder) params.set('sortOrder', args.sortOrder)
        if (args?.addedFrom) params.set('addedFrom', args.addedFrom)
        if (args?.addedTo) params.set('addedTo', args.addedTo)
        return `/leads?${params.toString()}`
      },
      providesTags: ['Lead'],
    }),
    searchRhetorik: builder.mutation<EnrichedProfileSearchResponse, ProfileSearchRequest>({
      query: (request) => ({ url: '/leads/search-rhetorik', method: 'POST', body: request }),
    }),
    generateSearchSpec: builder.mutation<Partial<ProfileSearchRequest>, { text: string }>({
      query: (body) => ({ url: '/leads/generate-search', method: 'POST', body }),
    }),
    autocomplete: builder.query<AutocompleteSuggestion[], { field: string; inputText: string }>({
      query: ({ field, inputText }) =>
        `/rhetorik/autocomplete?field=${encodeURIComponent(field)}&inputText=${encodeURIComponent(inputText)}`,
    }),
    importFromRhetorik: builder.mutation<Lead[], ProfileSearchRequest>({
      query: (request) => ({ url: '/leads/import', method: 'POST', body: request }),
      invalidatesTags: ['Lead'],
    }),
    importToCampaign: builder.mutation<
      { added: number; skipped: number },
      { campaignId: number; profileIds: string[] }
    >({
      query: ({ campaignId, profileIds }) => ({
        url: '/leads/import-to-campaign',
        method: 'POST',
        body: { campaignId, profileIds },
      }),
      invalidatesTags: ['Lead'],
    }),
    updateLeadStatus: builder.mutation<void, { id: number; status: number }>({
      query: ({ id, status }) => ({ url: `/leads/${id}/status`, method: 'PATCH', body: { status } }),
      invalidatesTags: ['Lead'],
    }),
    updateLead: builder.mutation<
      Lead,
      { id: number; firstName?: string; lastName?: string; email?: string; phone?: string | null; linkedInUrl?: string | null; company?: string | null; jobTitle?: string | null; location?: string | null; country?: string | null }
    >({
      query: ({ id, ...body }) => ({ url: `/leads/${id}`, method: 'PUT', body }),
      invalidatesTags: ['Lead'],
    }),
    refreshLeads: builder.mutation<{ updated: number }, { leadIds: number[] }>({
      query: (body) => ({ url: '/leads/refresh', method: 'POST', body }),
      invalidatesTags: ['Lead', 'OutreachMessage'],
    }),
    getCampaigns: builder.query<Campaign[], void>({
      query: () => '/campaigns',
      providesTags: ['Campaign'],
    }),
    getCampaign: builder.query<Campaign, number>({
      query: (id) => `/campaigns/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Campaign', id }],
    }),
    createCampaign: builder.mutation<Campaign, { name: string; description?: string; sequenceId?: number | null; channel?: number }>({
      query: (body) => ({ url: '/campaigns', method: 'POST', body }),
      invalidatesTags: ['Campaign'],
    }),
    updateCampaign: builder.mutation<
      void,
      { id: number; name: string; description?: string; sequenceId?: number | null; status: number; channel?: number }
    >({
      query: ({ id, ...body }) => ({ url: `/campaigns/${id}`, method: 'PUT', body }),
      invalidatesTags: (_result, _error, arg) => ['Campaign', 'OutreachMessage', { type: 'Campaign', id: arg.id }],
    }),
    runCampaign: builder.mutation<{ queued: boolean; alreadyRunning?: boolean }, number>({
      query: (id) => ({ url: `/campaigns/${id}/run`, method: 'POST' }),
      invalidatesTags: ['Campaign', 'OutreachMessage', 'Lead'],
    }),
    restartCampaign: builder.mutation<{ restarted: boolean; candidates: number }, number>({
      query: (id) => ({ url: `/campaigns/${id}/restart`, method: 'POST' }),
      invalidatesTags: ['Campaign', 'OutreachMessage', 'Lead'],
    }),
    addLeadsToCampaign: builder.mutation<
      { added: number; skipped: number },
      { campaignId: number; leadIds: number[] }
    >({
      query: ({ campaignId, leadIds }) => ({
        url: `/campaigns/${campaignId}/leads`,
        method: 'POST',
        body: { leadIds },
      }),
      invalidatesTags: (_result, _error, arg) => [
        'OutreachMessage',
        { type: 'Campaign', id: arg.campaignId },
      ],
    }),
    getMessages: builder.query<OutreachMessage[], number>({
      query: (campaignId) => `/campaigns/${campaignId}/messages`,
      providesTags: ['OutreachMessage'],
    }),
    createDraft: builder.mutation<
      OutreachMessage,
      { campaignId: number; leadId: number; subjectTemplate: string; bodyTemplate: string; channel: number }
    >({
      query: ({ campaignId, ...body }) => ({
        url: `/campaigns/${campaignId}/messages/drafts`,
        method: 'POST',
        body,
      }),
      invalidatesTags: ['OutreachMessage'],
    }),
    sendMessage: builder.mutation<void, { campaignId: number; messageId: number }>({
      query: ({ campaignId, messageId }) => ({
        url: `/campaigns/${campaignId}/messages/${messageId}/send`,
        method: 'POST',
      }),
      invalidatesTags: ['OutreachMessage', 'Lead'],
    }),
    markMessageReplied: builder.mutation<void, { campaignId: number; messageId: number }>({
      query: ({ campaignId, messageId }) => ({
        url: `/campaigns/${campaignId}/messages/${messageId}/mark-replied`,
        method: 'POST',
      }),
      invalidatesTags: ['OutreachMessage'],
    }),
    getCampaignReport: builder.query<CampaignReportSummary, number>({
      query: (campaignId) => `/reports/campaign/${campaignId}`,
    }),
    getCampaignReportCandidates: builder.query<CandidateReportResponse, number>({
      query: (campaignId) => `/reports/campaign/${campaignId}/candidates`,
    }),
    getSequences: builder.query<Sequence[], void>({
      query: () => '/sequences',
      providesTags: ['Sequence'],
    }),
    getSequence: builder.query<Sequence, number>({
      query: (id) => `/sequences/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Sequence', id }],
    }),
    createSequence: builder.mutation<Sequence, SequenceInput>({
      query: (body) => ({ url: '/sequences', method: 'POST', body }),
      invalidatesTags: ['Sequence'],
    }),
    updateSequence: builder.mutation<Sequence, { id: number } & SequenceInput>({
      query: ({ id, ...body }) => ({ url: `/sequences/${id}`, method: 'PUT', body }),
      invalidatesTags: (_result, _error, arg) => ['Sequence', { type: 'Sequence', id: arg.id }],
    }),
    deleteSequence: builder.mutation<void, number>({
      query: (id) => ({ url: `/sequences/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Sequence'],
    }),
    getPersonalisationOptions: builder.query<PersonalisationOption[], void>({
      query: () => '/sequences/personalisation-options',
    }),
    previewSequence: builder.mutation<
      SequencePreview,
      { leadId: number; subjectTemplate: string; bodyTemplate: string; channel: number; includeUnsubscribe: boolean; jobId?: number }
    >({
      query: (body) => ({ url: '/sequences/preview', method: 'POST', body }),
    }),
    getJobs: builder.query<Job[], void>({
      query: () => '/jobs',
      providesTags: ['Job'],
    }),
    getJob: builder.query<Job, number>({
      query: (id) => `/jobs/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Job', id }],
    }),
    createJob: builder.mutation<Job, JobInput>({
      query: (body) => ({ url: '/jobs', method: 'POST', body }),
      invalidatesTags: ['Job'],
    }),
    updateJob: builder.mutation<void, { id: number } & JobInput>({
      query: ({ id, ...body }) => ({ url: `/jobs/${id}`, method: 'PUT', body }),
      invalidatesTags: (_result, _error, arg) => ['Job', { type: 'Job', id: arg.id }],
    }),
    deleteJob: builder.mutation<void, number>({
      query: (id) => ({ url: `/jobs/${id}`, method: 'DELETE' }),
      invalidatesTags: ['Job'],
    }),
    generateJobSearch: builder.mutation<ProfileSearchRequest, number>({
      query: (id) => ({ url: `/jobs/${id}/generate-search`, method: 'POST' }),
    }),
    getOrganization: builder.query<OrganizationProfile, void>({
      query: () => '/organization',
      providesTags: ['Organization'],
    }),
    updateOrganization: builder.mutation<OrganizationProfile, Partial<OrganizationProfile>>({
      query: (body) => ({ url: '/organization', method: 'PUT', body }),
      invalidatesTags: ['Organization'],
    }),
    getPolicy: builder.query<PolicyGuardrails, void>({
      query: () => '/policy',
      providesTags: ['Policy'],
    }),
    updatePolicy: builder.mutation<PolicyGuardrails, Partial<PolicyGuardrails>>({
      query: (body) => ({ url: '/policy', method: 'PUT', body }),
      invalidatesTags: ['Policy'],
    }),
    generateContent: builder.mutation<GeneratedContent, GenerateContentRequest>({
      query: (body) => ({ url: '/sequences/generate-content', method: 'POST', body }),
    }),
    getLeadConsent: builder.query<LeadConsentResponse, number>({
      query: (leadId) => `/leads/${leadId}/consent`,
      providesTags: (_result, _error, leadId) => [{ type: 'Lead', id: leadId }],
    }),
    updateLeadConsent: builder.mutation<
      LeadConsentResponse,
      { leadId: number; channel: number; status: number; optInSource?: string; notes?: string }
    >({
      query: ({ leadId, ...body }) => ({ url: `/leads/${leadId}/consent`, method: 'PUT', body }),
      invalidatesTags: (_result, _error, arg) => [{ type: 'Lead', id: arg.leadId }],
    }),
    setPreferredChannel: builder.mutation<void, { leadId: number; channel: number | null }>({
      query: ({ leadId, channel }) => ({
        url: `/leads/${leadId}/consent/preferred-channel`,
        method: 'PUT',
        body: channel,
      }),
      invalidatesTags: (_result, _error, arg) => [{ type: 'Lead', id: arg.leadId }],
    }),
    getLeadProfile: builder.query<LeadProfile, number>({
      query: (leadId) => `/leads/${leadId}/profile`,
      providesTags: (_result, _error, leadId) => [{ type: 'Lead', id: leadId }],
    }),
    getLinkedInStatus: builder.query<LinkedInStatus, void>({
      query: () => '/linkedin/status',
    }),
    signInToLinkedIn: builder.mutation<{ signedIn: boolean }, void>({
      query: () => ({ url: '/linkedin/sign-in', method: 'POST' }),
    }),
    scottyChat: builder.mutation<ScottyChatResponse, { userPrompt: string; continuityKey?: string }>({
      query: (body) => ({ url: '/scotty/chat', method: 'POST', body }),
    }),
    scottyCall: builder.mutation<ScottyCallResponse, { continuityKey?: string }>({
      query: (body) => ({ url: '/scotty/call', method: 'POST', body }),
    }),
  }),
})

export const {
  useGetAuthStatusQuery,
  useSetupAdminMutation,
  useLoginMutation,
  useGetMeQuery,
  useUpdateMeMutation,
  useLogoutMutation,
  useSendTestEmailMutation,
  useGetMicrosoftStatusQuery,
  useLazyGetMicrosoftConnectUrlQuery,
  useDisconnectMicrosoftMutation,
  useCreateExtensionCodeMutation,
  useGetUsersQuery,
  useCreateUserMutation,
  useChangePasswordMutation,
  useGetUpcomingInterviewsQuery,
  useGetLeadInterviewsQuery,
  useLazyGetInterviewSlotsQuery,
  useBookInterviewMutation,
  useRescheduleInterviewMutation,
  useCancelInterviewMutation,
  useProcessInterviewTranscriptMutation,
  useUpdateInterviewRoleMutation,
  useGetLeadNotesQuery,
  useGetLeadsQuery,
  useSearchRhetorikMutation,
  useGenerateSearchSpecMutation,
  useLazyAutocompleteQuery,
  useImportFromRhetorikMutation,
  useImportToCampaignMutation,
  useUpdateLeadStatusMutation,
  useUpdateLeadMutation,
  useRefreshLeadsMutation,
  useGetCampaignsQuery,
  useGetCampaignQuery,
  useCreateCampaignMutation,
  useGetMessagesQuery,
  useGetCampaignReportQuery,
  useGetCampaignReportCandidatesQuery,
  useCreateDraftMutation,
  useSendMessageMutation,
  useUpdateCampaignMutation,
  useAddLeadsToCampaignMutation,
  useRunCampaignMutation,
  useRestartCampaignMutation,
  useMarkMessageRepliedMutation,
  useGetSequencesQuery,
  useGetSequenceQuery,
  useCreateSequenceMutation,
  useUpdateSequenceMutation,
  useDeleteSequenceMutation,
  useGetPersonalisationOptionsQuery,
  usePreviewSequenceMutation,
  useGetJobsQuery,
  useGetJobQuery,
  useCreateJobMutation,
  useUpdateJobMutation,
  useDeleteJobMutation,
  useGenerateJobSearchMutation,
  useGetOrganizationQuery,
  useUpdateOrganizationMutation,
  useGetPolicyQuery,
  useUpdatePolicyMutation,
  useGenerateContentMutation,
  useGetLeadConsentQuery,
  useUpdateLeadConsentMutation,
  useSetPreferredChannelMutation,
  useGetLeadProfileQuery,
  useScottyChatMutation,
  useScottyCallMutation,
  useGetLinkedInStatusQuery,
  useSignInToLinkedInMutation,
} = apiSlice

