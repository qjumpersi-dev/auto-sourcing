import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type {
  AutocompleteSuggestion,
  Campaign,
  Lead,
  OutreachMessage,
  ProfileSearchRequest,
  ProfileSearchResponse,
} from '@/types/models'

export const apiSlice = createApi({
  reducerPath: 'api',
  baseQuery: fetchBaseQuery({ baseUrl: '/api' }),
  tagTypes: ['Lead', 'Campaign', 'OutreachMessage'],
  endpoints: (builder) => ({
    getLeads: builder.query<Lead[], void>({
      query: () => '/leads',
      providesTags: ['Lead'],
    }),
    searchRhetorik: builder.mutation<ProfileSearchResponse, ProfileSearchRequest>({
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
    updateLeadStatus: builder.mutation<void, { id: number; status: number }>({
      query: ({ id, status }) => ({ url: `/leads/${id}/status`, method: 'PATCH', body: status }),
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
    createCampaign: builder.mutation<Campaign, { name: string; description?: string }>({
      query: (body) => ({ url: '/campaigns', method: 'POST', body }),
      invalidatesTags: ['Campaign'],
    }),
    getMessages: builder.query<OutreachMessage[], number>({
      query: (campaignId) => `/campaigns/${campaignId}/messages`,
      providesTags: ['OutreachMessage'],
    }),
    createDraft: builder.mutation<
      OutreachMessage,
      { campaignId: number; leadId: number; subjectTemplate: string; bodyTemplate: string }
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
  }),
})

export const {
  useGetLeadsQuery,
  useSearchRhetorikMutation,
  useGenerateSearchSpecMutation,
  useLazyAutocompleteQuery,
  useImportFromRhetorikMutation,
  useUpdateLeadStatusMutation,
  useGetCampaignsQuery,
  useGetCampaignQuery,
  useCreateCampaignMutation,
  useGetMessagesQuery,
  useCreateDraftMutation,
  useSendMessageMutation,
} = apiSlice
