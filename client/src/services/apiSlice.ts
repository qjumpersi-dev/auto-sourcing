import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type {
  Campaign,
  Lead,
  OutreachMessage,
  RhetorikSearchRequest,
  RhetorikSearchResponse,
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
    searchRhetorik: builder.mutation<RhetorikSearchResponse, RhetorikSearchRequest>({
      query: (request) => ({ url: '/leads/search-rhetorik', method: 'POST', body: request }),
    }),
    importFromRhetorik: builder.mutation<Lead[], RhetorikSearchRequest>({
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
    updateCampaign: builder.mutation<void, { id: number; name: string; description?: string; status: number }>({
      query: ({ id, ...body }) => ({ url: `/campaigns/${id}`, method: 'PUT', body }),
      invalidatesTags: (_result, _error, arg) => ['Campaign', { type: 'Campaign', id: arg.id }],
    }),
    getMessages: builder.query<OutreachMessage[], number>({
      query: (campaignId) => `/campaigns/${campaignId}/messages`,
      providesTags: ['OutreachMessage'],
    }),
    createDraft: builder.mutation<OutreachMessage, { campaignId: number; leadId: number; subjectTemplate: string; bodyTemplate: string }>({
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
  useImportFromRhetorikMutation,
  useUpdateLeadStatusMutation,
  useGetCampaignsQuery,
  useGetCampaignQuery,
  useCreateCampaignMutation,
  useUpdateCampaignMutation,
  useGetMessagesQuery,
  useCreateDraftMutation,
  useSendMessageMutation,
} = apiSlice
