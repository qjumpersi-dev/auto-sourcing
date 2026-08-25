import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Loader2, Plus } from 'lucide-react'
import { useCreateCampaignMutation, useGetCampaignsQuery } from '@/services/apiSlice'
import { campaignStatusLabels } from '@/types/models'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'

interface CampaignFormValues {
  subjectTemplate?: string
  bodyTemplate?: string
  name: string
  description?: string
}

export function CampaignsPage({ onOpenCampaign }: { onOpenCampaign: (id: number) => void }) {
  const { data: campaigns = [], isLoading } = useGetCampaignsQuery()
  const [createCampaign, { isLoading: creating }] = useCreateCampaignMutation()
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CampaignFormValues>()
  const [error, setError] = useState<string | null>(null)

  const onSubmit = handleSubmit(async (values) => {
    setError(null)
    try {
      await createCampaign(values).unwrap()
      reset()
    } catch (e) {
      setError('Could not create the campaign. Please try again.')
    }
  })

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <Card className="lg:col-span-1">
        <CardHeader>
          <CardTitle>New campaign</CardTitle>
          <CardDescription>Group leads into an outreach campaign.</CardDescription>
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
              {errors.name && (
                <p className="text-xs text-destructive">{errors.name.message}</p>
              )}
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
              <Label htmlFor="subjectTemplate">Subject template</Label>
              <Input
                id="subjectTemplate"
                placeholder="Quick question, {'{{FirstName}}'}"
                {...register('subjectTemplate')}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="bodyTemplate">Body template</Label>
              <Textarea id="bodyTemplate" rows={5} {...register('bodyTemplate')} />
              <p className="text-xs text-muted-foreground">
                Used to auto-draft messages when you add leads to this campaign. Supports
                {'{{FirstName}}'}, {'{{Company}}'}, {'{{JobTitle}}'}.
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
          <CardTitle>Campaigns</CardTitle>
          <CardDescription>Click a campaign to manage its outreach messages.</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-muted-foreground">LoadingÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦</p>
          ) : campaigns.length === 0 ? (
            <p className="text-sm text-muted-foreground">No campaigns yet.</p>
          ) : (
            <ul className="divide-y">
              {campaigns.map((campaign) => (
                <li key={campaign.id}>
                  <button
                    type="button"
                    onClick={() => onOpenCampaign(campaign.id)}
                    className="flex w-full items-center justify-between rounded-lg px-3 py-3 text-left transition-colors hover:bg-accent"
                  >
                    <div>
                      <p className="font-medium">{campaign.name}</p>
                      {campaign.description && (
                        <p className="text-sm text-muted-foreground">{campaign.description}</p>
                      )}
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
