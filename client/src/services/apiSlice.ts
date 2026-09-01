import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type {
  AutocompleteSuggestion,
  Campaign,
  EnrichedProfileSearchResponse,
  Lead,
  LinkedInStatus,
  OutreachMessage,
  PaginatedLeads,
  ProfileSearchRequest,
  ScottyCallResponse,
  ScottyChatResponse,
} from '@/types/models'

export const apiSlice = createApi({
  reducerPath: 'api',
  baseQuery: fetchBaseQuery({ baseUrl: '/api' }),
  tagTypes: ['Lead', 'Campaign', 'OutreachMessage'],
  endpoints: (builder) => ({
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
    getCampaigns: builder.query<Campaign[], void>({
      query: () => '/campaigns',
      providesTags: ['Campaign'],
    }),
    getCampaign: builder.query<Campaign, number>({
      query: (id) => `/campaigns/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Campaign', id }],
    }),
    createCampaign: builder.mutation<Campaign, { name: string; description?: string; subjectTemplate?: string; bodyTemplate?: string; channel?: number }>({
      query: (body) => ({ url: '/campaigns', method: 'POST', body }),
      invalidatesTags: ['Campaign'],
    }),
    updateCampaign: builder.mutation<
      void,
      { id: number; name: string; description?: string; subjectTemplate?: string; bodyTemplate?: string; status: number; channel?: number }
    >({
      query: ({ id, ...body }) => ({ url: `/campaigns/${id}`, method: 'PUT', body }),
      invalidatesTags: (_result, _error, arg) => ['Campaign', { type: 'Campaign', id: arg.id }],
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
  useGetLeadsQuery,
  useSearchRhetorikMutation,
  useGenerateSearchSpecMutation,
  useLazyAutocompleteQuery,
  useImportFromRhetorikMutation,
  useImportToCampaignMutation,
  useUpdateLeadStatusMutation,
  useGetCampaignsQuery,
  useGetCampaignQuery,
  useCreateCampaignMutation,
  useGetMessagesQuery,
  useCreateDraftMutation,
  useSendMessageMutation,
  useUpdateCampaignMutation,
  useAddLeadsToCampaignMutation,
  useScottyChatMutation,
  useScottyCallMutation,
  useGetLinkedInStatusQuery,
  useSignInToLinkedInMutation,
} = apiSlice

