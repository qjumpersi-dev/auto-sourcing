import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Loader2, Plus } from 'lucide-react'
import {
  useCreateCampaignMutation,
  useGetCampaignsQuery,
  useGetClientsQuery,
  useGetJobsQuery,
  useGetSequencesQuery,
} from '@/services/apiSlice'
import { campaignStatusLabels } from '@/types/models'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'

interface CampaignFormValues {
  name: string
  description?: string
  sequenceId?: string
}

export function CampaignsPage({ onOpenCampaign }: { onOpenCampaign: (id: number) => void }) {
  const { data: campaigns = [], isLoading } = useGetCampaignsQuery()
  const { data: sequences = [] } = useGetSequencesQuery()
  const { data: jobs = [] } = useGetJobsQuery()
  const { data: clients = [] } = useGetClientsQuery()
  const [createCampaign, { isLoading: creating }] = useCreateCampaignMutation()
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CampaignFormValues>()
  const [error, setError] = useState<string | null>(null)
  const [clientFilter, setClientFilter] = useState('')

  const sequenceNameById = new Map(sequences.map((sequence) => [sequence.id, sequence.name]))
  const jobById = new Map(jobs.map((job) => [job.id, job]))
  const clientNameById = new Map(clients.map((client) => [client.id, client.name]))

  const campaignClientName = (campaign: (typeof campaigns)[number]): string | null => {
    const job = campaign.jobId ? jobById.get(campaign.jobId) : undefined
    if (!job?.clientId) {
      return null
    }
    return clientNameById.get(job.clientId) ?? null
  }

  const visibleCampaigns = clientFilter
    ? campaigns.filter((campaign) => {
        const job = campaign.jobId ? jobById.get(campaign.jobId) : undefined
        return String(job?.clientId ?? '') === clientFilter
      })
    : campaigns

  const onSubmit = handleSubmit(async (values) => {
    setError(null)
    try {
      await createCampaign({
        name: values.name,
        description: values.description || undefined,
        sequenceId: values.sequenceId ? Number(values.sequenceId) : null,
        channel: 0,
      }).unwrap()
      reset()
    } catch {
      setError('Could not create the campaign. Please try again.')
    }
  })

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <Card className="lg:col-span-1">
        <CardHeader>
          <CardTitle>New campaign</CardTitle>
          <CardDescription>Group leads and pick the sequence they will receive.</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={onSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="name">Name</Label>
              <Input
                id="name"
                placeholder="NZ recruiters - August"
                {...register('name', { required: 'Name is required' })}
              />
              {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                rows={2}
                placeholder="What is this campaign about?"
                {...register('description')}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="sequenceId">Sequence</Label>
              <Select id="sequenceId" defaultValue="" {...register('sequenceId')}>
                <option value="">No sequence yet</option>
                {sequences.map((sequence) => (
                  <option key={sequence.id} value={sequence.id}>
                    {sequence.name} ({sequence.steps.length} step
                    {sequence.steps.length === 1 ? '' : 's'})
                  </option>
                ))}
              </Select>
              <p className="text-xs text-muted-foreground">
                Emails and InMails are written in the Sequences area. You can also change this later.
              </p>
            </div>
            {error && <p className="text-xs text-destructive">{error}</p>}
            <Button type="submit" disabled={creating} className="w-full">
              {creating ? <Loader2 className="animate-spin" /> : <Plus />}
              Create campaign
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card className="lg:col-span-2">
        <CardHeader>
          <div className="flex flex-wrap items-end justify-between gap-3">
            <div>
              <CardTitle>Campaigns</CardTitle>
              <CardDescription>Click a campaign to choose its sequence and run it.</CardDescription>
            </div>
            <div className="space-y-1.5">
              <Label>Filter by client</Label>
              <Select
                className="w-56"
                value={clientFilter}
                onChange={(e) => setClientFilter(e.target.value)}
              >
                <option value="">All clients</option>
                {clients.map((client) => (
                  <option key={client.id} value={client.id}>
                    {client.name}
                  </option>
                ))}
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-muted-foreground">Loading...</p>
          ) : visibleCampaigns.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              {campaigns.length === 0 ? 'No campaigns yet.' : 'No campaigns for this client.'}
            </p>
          ) : (
            <ul className="divide-y">
              {visibleCampaigns.map((campaign) => (
                <li key={campaign.id}>
                  <button
                    type="button"
                    onClick={() => onOpenCampaign(campaign.id)}
                    className="flex w-full items-center justify-between rounded-lg px-3 py-3 text-left transition-colors hover:bg-accent"
                  >
                    <div>
                      <p className="font-medium">{campaign.name}</p>
                      <p className="text-sm text-muted-foreground">
                        {[
                          campaignClientName(campaign),
                          campaign.jobId ? jobById.get(campaign.jobId)?.title : null,
                          campaign.sequenceId
                            ? `Sequence: ${sequenceNameById.get(campaign.sequenceId) ?? `#${campaign.sequenceId}`}`
                            : 'No sequence selected',
                          campaign.description,
                        ]
                          .filter(Boolean)
                          .join(' · ')}
                      </p>
                    </div>
                    <Badge variant="secondary">
                      {campaignStatusLabels[campaign.status] ?? 'Unknown'}
                    </Badge>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
