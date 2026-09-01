import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { ArrowLeft, Loader2, MailPlus, Save, Send } from 'lucide-react'
import {
  useCreateDraftMutation,
  useGetCampaignQuery,
  useUpdateCampaignMutation,
  useGetLeadsQuery,
  useGetMessagesQuery,
  useSendMessageMutation,
  useGetLinkedInStatusQuery,
  useSignInToLinkedInMutation,
} from '@/services/apiSlice'
import {
  messageStatusLabels,
  OutreachMessageStatus,
  outreachChannelLabels,
  OutreachChannel,
} from '@/types/models'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Textarea } from '@/components/ui/textarea'

interface DraftFormValues {
  leadId: string
  subjectTemplate: string
  bodyTemplate: string
  channel: string
}

function messageVariant(status: number) {
  switch (status) {
    case OutreachMessageStatus.Sent:
      return 'success'
    case OutreachMessageStatus.Failed:
    case OutreachMessageStatus.Bounced:
      return 'destructive'
    case OutreachMessageStatus.Queued:
      return 'warning'
    default:
      return 'secondary'
  }
}

export function CampaignDetailPage({
  campaignId,
  onBack,
}: {
  campaignId: number
  onBack: () => void
}) {
  const { data: campaign } = useGetCampaignQuery(campaignId)
  const { data: messages = [] } = useGetMessagesQuery(campaignId)
  const { data: leads = { items: [] } } = useGetLeadsQuery({ page: 1, pageSize: 100 })
  const { data: linkedInStatus, refetch: refetchLinkedInStatus } = useGetLinkedInStatusQuery()
  const [signInToLinkedIn, { isLoading: signingIn }] = useSignInToLinkedInMutation()
  const [createDraft, { isLoading: creating }] = useCreateDraftMutation()
  const [sendMessage, { isLoading: sending }] = useSendMessageMutation()
  const [draftError, setDraftError] = useState<string | null>(null)
  const [templateState, setTemplateState] = useState<{ subject: string; body: string; channel: number } | null>(null)
  const [savedTemplates, setSavedTemplates] = useState(false)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<DraftFormValues>()

  useEffect(() => {
    setDraftError(null)
  }, [campaignId])
  useEffect(() => {
    if (campaign && templateState === null) {
      setTemplateState({
        subject: campaign.subjectTemplate ?? '',
        body: campaign.bodyTemplate ?? '',
        channel: campaign.channel ?? OutreachChannel.Email,
      })
    }
  }, [campaign, templateState])

  const [updateCampaign, { isLoading: savingTemplates }] = useUpdateCampaignMutation()

  const onSaveTemplates = async () => {
    if (!campaign || !templateState) return
    setSavedTemplates(false)
    try {
      await updateCampaign({
        id: campaign.id,
        name: campaign.name,
        description: campaign.description ?? undefined,
        status: campaign.status,
        channel: templateState.channel,
        subjectTemplate: templateState.subject,
        bodyTemplate: templateState.body,
      }).unwrap()
      setSavedTemplates(true)
    } catch {
      setDraftError('Could not save templates.')
    }
  }

  const onSubmit = handleSubmit(async (values) => {
    setDraftError(null)
    try {
      await createDraft({
        campaignId,
        leadId: Number(values.leadId),
        channel: Number(values.channel),
        subjectTemplate: values.subjectTemplate,
        bodyTemplate: values.bodyTemplate,
      }).unwrap()
      reset()
    } catch (e) {
      setDraftError('Could not create the draft.')
    }
  })

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={onBack} aria-label="Back to campaigns">
          <ArrowLeft />
        </Button>
        <div>
          <h2 className="text-xl font-semibold">{campaign?.name ?? 'Campaign'}</h2>
          {campaign?.description && (
            <p className="text-sm text-muted-foreground">{campaign.description}</p>
          )}
        </div>
      </div>

      {linkedInStatus && !linkedInStatus.signedIn && (
        <div className="rounded-lg border border-destructive/40 bg-destructive/10 p-3 text-sm">
          <p className="font-medium text-destructive">LinkedIn InMail needs a signed-in session</p>
          <p className="text-muted-foreground">
            A Chromium window will open on the API machine. Log into LinkedIn in that window, then the app
            picks up the session automatically.{' '}
            {linkedInStatus.dryRun
              ? 'Dry-run mode is on - messages will be prepared in the composer but not sent.'
              : ''}
          </p>
          <div className="mt-2">
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={signingIn}
              onClick={async () => {
                await signInToLinkedIn()
                refetchLinkedInStatus()
              }}
            >
              {signingIn ? <Loader2 className="animate-spin" /> : null}
              Log in to LinkedIn
            </Button>
          </div>
        </div>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Campaign templates</CardTitle>
          <CardDescription>
            Used to auto-draft messages when leads are added to this campaign. Supports{' '}
            {'{{FirstName}}'}, {'{{LastName}}'}, {'{{Company}}'}, {'{{JobTitle}}'}.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {templateState && (
            <div className="space-y-4">
              <div className="space-y-1.5">
                <Label htmlFor="tplChannel">Channel</Label>
                <Select
                  id="tplChannel"
                  value={String(templateState.channel)}
                  onChange={(e) => {
                    setTemplateState({ ...templateState, channel: Number(e.target.value) })
                    setSavedTemplates(false)
                  }}
                >
                  <option value={OutreachChannel.Email}>{outreachChannelLabels[OutreachChannel.Email]}</option>
                  <option value={OutreachChannel.LinkedIn}>{outreachChannelLabels[OutreachChannel.LinkedIn]}</option>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="tplSubject">Subject template</Label>
                <Input
                  id="tplSubject"
                  value={templateState.subject}
                  onChange={(e) => {
                    setTemplateState({ ...templateState, subject: e.target.value })
                    setSavedTemplates(false)
                  }}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="tplBody">Body template</Label>
                <Textarea
                  id="tplBody"
                  rows={6}
                  value={templateState.body}
                  onChange={(e) => {
                    setTemplateState({ ...templateState, body: e.target.value })
                    setSavedTemplates(false)
                  }}
                />
              </div>
              <div className="flex items-center gap-3">
                <Button type="button" variant="outline" disabled={savingTemplates} onClick={onSaveTemplates}>
                  {savingTemplates ? <Loader2 className="animate-spin" /> : <Save />}
                  Save templates
                </Button>
                {savedTemplates && (
                  <span className="text-xs text-muted-foreground">Templates saved.</span>
                )}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Create draft</CardTitle>
          <CardDescription>
            Personalise with {'{{FirstName}}'}, {'{{LastName}}'}, {'{{Company}}'},{' '}
            {'{{JobTitle}}'}.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={onSubmit} className="space-y-4">
            <div className="space-y-1.5 max-w-sm">
              <Label htmlFor="leadId">Lead</Label>
              <Select {...register('leadId', { required: true })}>
                <option value="">Select a leadÃƒÂ¢Ã¢â€šÂ¬Ã‚Â¦</option>
                {leads.items.map((lead) => (
                  <option key={lead.id} value={lead.id}>
                    {lead.firstName} {lead.lastName}
                    {lead.company ? ` - ${lead.company}` : ''}
                  </option>
                ))}
              </Select>
              {errors.leadId && <p className="text-xs text-destructive">Pick a lead.</p>}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="draftChannel">Channel</Label>
              <Select id="draftChannel" defaultValue="0" {...register('channel', { required: true })}>
                <option value={OutreachChannel.Email}>{outreachChannelLabels[OutreachChannel.Email]}</option>
                <option value={OutreachChannel.LinkedIn}>{outreachChannelLabels[OutreachChannel.LinkedIn]}</option>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="subject">Subject template</Label>
              <Input
                id="subject"
                placeholder="Quick question, {'{{FirstName}}'}"
                {...register('subjectTemplate', { required: true })}
              />
              {errors.subjectTemplate && (
                <p className="text-xs text-destructive">Subject is required.</p>
              )}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="body">Body template</Label>
              <Textarea id="body" rows={6} {...register('bodyTemplate', { required: true })} />
              {errors.bodyTemplate && (
                <p className="text-xs text-destructive">Body is required.</p>
              )}
            </div>
            {draftError && <p className="text-xs text-destructive">{draftError}</p>}
            <Button type="submit" disabled={creating}>
              {creating ? <Loader2 className="animate-spin" /> : <MailPlus />}
              Save draft
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Messages ({messages.length})</CardTitle>
          <CardDescription>Draft, send and track outreach per lead.</CardDescription>
        </CardHeader>
        <CardContent>
          {messages.length === 0 ? (
            <p className="text-sm text-muted-foreground">No messages yet.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Lead</TableHead>
                  <TableHead>Channel</TableHead>
                  <TableHead>Subject</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Sent</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {messages.map((message) => (
                  <TableRow key={message.id}>
                    <TableCell className="font-medium">
                      {message.lead
                        ? `${message.lead.firstName} ${message.lead.lastName}`
                        : `#${message.leadId}`}
                      {message.errorMessage && (
                        <span className="block text-xs text-destructive">
                          {message.errorMessage}
                        </span>
                      )}
                    </TableCell>
                    <TableCell>
                      <Badge variant={message.channel === OutreachChannel.LinkedIn ? 'outline' : 'secondary'}>
                        {outreachChannelLabels[message.channel] ?? message.channel}
                      </Badge>
                    </TableCell>
                    <TableCell>{message.subject ?? 'ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â'}</TableCell>
                    <TableCell>
                      <Badge variant={messageVariant(message.status)}>
                        {messageStatusLabels[message.status]}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      {message.sentAt
                        ? new Date(message.sentAt).toLocaleString()
                        : 'ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Â'}
                    </TableCell>
                    <TableCell className="text-right">
                      {message.status === OutreachMessageStatus.Draft ||
                      message.status === OutreachMessageStatus.Failed ? (
                        <Button
                          size="sm"
                          variant="outline"
                          disabled={sending}
                          onClick={() =>
                            sendMessage({ campaignId, messageId: message.id })
                          }
                        >
                          {sending ? (
                            <Loader2 className="animate-spin" />
                          ) : (
                            <Send />
                          )}
                          Send
                        </Button>
                      ) : null}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

