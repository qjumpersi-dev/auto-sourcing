import { useEffect, useRef, useState, type ReactNode } from 'react'
import {
  ArrowLeft,
  Check,
  CircleCheck,
  Loader2,
  MailOpen,
  MessageSquareReply,
  MousePointerClick,
  Play,
  RefreshCw,
  RotateCcw,
  Save,
  Send,
  Users,
} from 'lucide-react'
import {
  useGetCampaignQuery,
  useGetMessagesQuery,
  useGetSequencesQuery,
  useGetLinkedInStatusQuery,
  useSignInToLinkedInMutation,
  useUpdateCampaignMutation,
  useSendMessageMutation,
  useMarkMessageRepliedMutation,
  useRunCampaignMutation,
  useRestartCampaignMutation,
  useRefreshLeadsMutation,
} from '@/services/apiSlice'
import { LeadProfileModal } from '@/features/leads/LeadProfileModal'
import {
  messageStatusLabels,
  OutreachMessageStatus,
  outreachChannelLabels,
  OutreachChannel,
} from '@/types/models'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { formatDate, formatDateTime } from '@/lib/formatDate'

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
  const [polling, setPolling] = useState(false)
  const seenRunning = useRef(false)
  const { data: campaign } = useGetCampaignQuery(campaignId, {
    pollingInterval: polling ? 3000 : 0,
    skipPollingIfUnfocused: true,
  })
  const { data: messages = [] } = useGetMessagesQuery(campaignId, {
    pollingInterval: polling ? 3000 : 0,
    skipPollingIfUnfocused: true,
  })
  const { data: sequences = [] } = useGetSequencesQuery()
  const { data: linkedInStatus, refetch: refetchLinkedInStatus } = useGetLinkedInStatusQuery()
  const [signInToLinkedIn, { isLoading: signingIn }] = useSignInToLinkedInMutation()
  const [updateCampaign, { isLoading: savingSequence }] = useUpdateCampaignMutation()
  const [sendMessage] = useSendMessageMutation()
  const [markReplied] = useMarkMessageRepliedMutation()
  const [runCampaign, { isLoading: starting }] = useRunCampaignMutation()
  const [restartCampaign, { isLoading: restarting }] = useRestartCampaignMutation()

  const [selectedSequenceId, setSelectedSequenceId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [savedMessage, setSavedMessage] = useState<string | null>(null)
  const [runResult, setRunResult] = useState<string | null>(null)
  const [sendError, setSendError] = useState<string | null>(null)
  const [sendingId, setSendingId] = useState<number | null>(null)
  const [profileLead, setProfileLead] = useState<any>(null)
  const [selectedLeadIds, setSelectedLeadIds] = useState<Set<number>>(new Set())
  const [refreshLeads, { isLoading: refreshing }] = useRefreshLeadsMutation()
  const [refreshMessage, setRefreshMessage] = useState<string | null>(null)

  useEffect(() => {
    setSelectedSequenceId(campaign?.sequenceId ? String(campaign.sequenceId) : '')
    setRunResult(null)
  }, [campaignId, campaign?.sequenceId])

  useEffect(() => {
    if (campaign?.isRunning) {
      seenRunning.current = true
      setPolling(true)
    } else if (seenRunning.current) {
      seenRunning.current = false
      setPolling(false)
      setRunResult('Campaign run finished.')
    }
  }, [campaign?.isRunning])

  const selectedSequence = sequences.find((sequence) => String(sequence.id) === selectedSequenceId)

  const candidateIds = new Set(messages.map((message) => message.leadId))
  const openedCount = new Set(messages.filter((m) => m.openedAt).map((m) => m.leadId)).size
  const clickedCount = new Set(messages.filter((m) => m.clickedAt).map((m) => m.leadId)).size
  const repliedCount = new Set(messages.filter((m) => m.repliedAt).map((m) => m.leadId)).size

  const onSaveSequence = async () => {
    if (!campaign) return
    setError(null)
    setSavedMessage(null)
    try {
      await updateCampaign({
        id: campaign.id,
        name: campaign.name,
        description: campaign.description ?? undefined,
        status: campaign.status,
        channel: campaign.channel,
        sequenceId: selectedSequenceId ? Number(selectedSequenceId) : null,
      }).unwrap()
      setSavedMessage('Sequence saved to this campaign.')
    } catch {
      setError('Could not save the sequence.')
    }
  }

  const onRestart = async () => {
    if (!window.confirm('Restart this campaign? This deletes all messages and re-sends step 1 to every candidate.')) return
    setError(null)
    setRunResult(null)
    try {
      const result = await restartCampaign(campaignId).unwrap()
      setRunResult(`Campaign restarted. Step 1 is being re-sent to ${result.candidates} candidate(s) in the background.`)
    } catch (e) {
      const data = (e as { data?: { error?: string } })?.data
      setError(data?.error ?? 'Could not restart the campaign.')
    }
  }

  const onRun = async () => {
    setError(null)
    setRunResult(null)
    try {
      const result = await runCampaign(campaignId).unwrap()
      if (result.alreadyRunning) {
        setRunResult('This campaign is already running in the background.')
      } else {
        setRunResult('Campaign running in the background. This page will update when it finishes.')
      }
    } catch (e) {
      const data = (e as { data?: { error?: string } })?.data
      setError(data?.error ?? 'Could not start the campaign.')
    }
  }

  const onSend = async (messageId: number) => {
    setSendError(null)
    setSendingId(messageId)
    try {
      await sendMessage({ campaignId, messageId }).unwrap()
    } catch (e) {
      const data = (e as { data?: { error?: string } })?.data
      setSendError(data?.error ?? 'Could not send the message. Check the API is running and LinkedIn is signed in.')
    } finally {
      setSendingId(null)
    }
  }

  const uniqueLeadIds = [...new Set(messages.map((m) => m.leadId))]
  const allSelected = uniqueLeadIds.length > 0 && uniqueLeadIds.every((id) => selectedLeadIds.has(id))

  const toggleAll = () => {
    if (allSelected) {
      setSelectedLeadIds(new Set())
    } else {
      setSelectedLeadIds(new Set(uniqueLeadIds))
    }
  }

  const toggleLead = (id: number) => {
    setSelectedLeadIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const onRefresh = async () => {
    const leadIds = selectedLeadIds.size > 0 ? [...selectedLeadIds] : uniqueLeadIds
    if (leadIds.length === 0) return
    setRefreshMessage(null)
    try {
      const result = await refreshLeads({ leadIds }).unwrap()
      setRefreshMessage(`Refreshed ${result.updated} candidate(s) from Rhetorik.`)
    } catch {
      setRefreshMessage('Could not refresh candidates.')
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center gap-3">
        <Button variant="ghost" size="icon" onClick={onBack} aria-label="Back to campaigns">
          <ArrowLeft />
        </Button>
        <div className="flex-1">
          <h2 className="text-xl font-semibold">{campaign?.name ?? 'Campaign'}</h2>
          {campaign?.description && (
            <p className="text-sm text-muted-foreground">{campaign.description}</p>
          )}
        </div>
        <Button onClick={onRun} disabled={starting || campaign?.isRunning || !campaign?.sequenceId}>
          {starting ? <Loader2 className="animate-spin" /> : <Play />}
          {campaign?.isRunning ? 'Campaign running' : 'Run campaign'}
        </Button>
        <Button
          variant="outline"
          onClick={onRestart}
          disabled={restarting || campaign?.isRunning || !campaign?.sequenceId}
        >
          {restarting ? <Loader2 className="animate-spin" /> : <RotateCcw />}
          Restart &amp; run
        </Button>
      </div>

      {error && (
        <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}
      {runResult && (
        <p className="flex items-center gap-2 rounded-md border border-emerald-300 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
          <CircleCheck className="h-4 w-4" />
          {runResult}
        </p>
      )}

      {linkedInStatus && linkedInStatus.available === false && linkedInStatus.mode === 'Local' && (
        <div className="rounded-lg border bg-muted/40 p-3 text-sm text-muted-foreground">
          LinkedIn steps are queued and sent by the LinkedIn worker or browser extension. Make sure it is
          running and signed in to LinkedIn. Email and SMS outreach send normally.
        </div>
      )}

      {linkedInStatus && linkedInStatus.available === false && linkedInStatus.mode !== 'Local' && (
        <div className="rounded-lg border bg-muted/40 p-3 text-sm text-muted-foreground">
          LinkedIn automation is not available in this environment. Email and SMS outreach still work.
        </div>
      )}

      {linkedInStatus && !linkedInStatus.signedIn && linkedInStatus.available !== false && (
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
          <CardTitle>Sequence</CardTitle>
          <CardDescription>
            Choose the sequence this campaign sends. Messages and InMails are written in the Sequences
            area.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap items-end gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="campaignSequence">Sequence</Label>
              <Select
                id="campaignSequence"
                className="w-80"
                value={selectedSequenceId}
                onChange={(e) => {
                  setSelectedSequenceId(e.target.value)
                  setSavedMessage(null)
                }}
              >
                <option value="">No sequence</option>
                {sequences.map((sequence) => (
                  <option key={sequence.id} value={sequence.id}>
                    {sequence.name} ({sequence.steps.length} step
                    {sequence.steps.length === 1 ? '' : 's'})
                  </option>
                ))}
              </Select>
            </div>
            <Button type="button" variant="outline" disabled={savingSequence} onClick={onSaveSequence}>
              {savingSequence ? <Loader2 className="animate-spin" /> : <Save />}
              Save sequence
            </Button>
            {savedMessage && <span className="pb-2 text-xs text-muted-foreground">{savedMessage}</span>}
          </div>

          {selectedSequence && (
            <ol className="space-y-1.5 rounded-md border bg-muted/30 p-3 text-sm">
              {selectedSequence.steps.map((step, index) => (
                <li key={step.id} className="flex items-center gap-2">
                  <span className="flex h-5 w-5 items-center justify-center rounded-full bg-primary/10 text-xs font-medium text-primary">
                    {index + 1}
                  </span>
                  <span className="font-medium">{step.name}</span>
                  <Badge variant="outline">
                    {outreachChannelLabels[step.channel] ?? 'Unknown'}
                  </Badge>
                  {index > 0 && (
                    <span className="text-xs text-muted-foreground">+{step.delayDays}d</span>
                  )}
                </li>
              ))}
            </ol>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-4 sm:grid-cols-4">
        <Stat icon={<Users />} label="Candidates" value={candidateIds.size} />
        <Stat icon={<MailOpen />} label="Opened" value={openedCount} />
        <Stat icon={<MousePointerClick />} label="Clicked a link" value={clickedCount} />
        <Stat icon={<MessageSquareReply />} label="Replied" value={repliedCount} />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Messages ({messages.length})</CardTitle>
          <CardDescription>
            Each candidate's progress through the sequence, with opens, clicks and replies.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="mb-3 flex items-center gap-2">
            <Button size="sm" variant="outline" onClick={onRefresh} disabled={refreshing || messages.length === 0}>
              {refreshing ? <Loader2 className="animate-spin" /> : <RefreshCw />}
              Refresh from Rhetorik
            </Button>
            {refreshMessage && <span className="text-xs text-muted-foreground">{refreshMessage}</span>}
          </div>
          {sendError && (
            <p className="mb-3 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {sendError}
            </p>
          )}
          {messages.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No messages yet - add candidates to this campaign, then run it.
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10">
                    <input
                      type="checkbox"
                      aria-label="Select all candidates"
                      checked={allSelected}
                      onChange={toggleAll}
                    />
                  </TableHead>
                  <TableHead>Candidate</TableHead>
                  <TableHead>Step</TableHead>
                  <TableHead>Channel</TableHead>
                  <TableHead>Subject</TableHead>
                  <TableHead>Opened</TableHead>
                  <TableHead>Clicked</TableHead>
                  <TableHead>Replied</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Sent</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {messages.map((message) => (
                  <TableRow key={message.id}>
                    <TableCell>
                      <input
                        type="checkbox"
                        aria-label="Select candidate"
                        checked={selectedLeadIds.has(message.leadId)}
                        onChange={() => toggleLead(message.leadId)}
                      />
                    </TableCell>
                    <TableCell className="font-medium">
                      {message.lead ? (
                        <button
                          type="button"
                          className="text-blue-600 hover:underline"
                          onClick={() => setProfileLead(message.lead!)}
                        >
                          {message.lead.firstName} {message.lead.lastName}
                        </button>
                      ) : (
                        `#${message.leadId}`
                      )}
                      {message.errorMessage && (
                        <span className="block text-xs text-destructive">
                          {message.errorMessage}
                        </span>
                      )}
                    </TableCell>
                    <TableCell>
                      {message.stepOrder ? `Step ${message.stepOrder}` : '—'}
                    </TableCell>
                    <TableCell>
                      <Badge variant={message.channel === OutreachChannel.LinkedIn ? 'outline' : 'secondary'}>
                        {outreachChannelLabels[message.channel] ?? message.channel}
                      </Badge>
                    </TableCell>
                    <TableCell>{message.subject ?? '—'}</TableCell>
                    <TableCell>
                      <TrackingCell value={message.openedAt} />
                    </TableCell>
                    <TableCell>
                      <TrackingCell value={message.clickedAt} />
                    </TableCell>
                    <TableCell>
                      <TrackingCell value={message.repliedAt} />
                    </TableCell>
                    <TableCell>
                      <Badge variant={messageVariant(message.status)}>
                        {messageStatusLabels[message.status]}
                      </Badge>
                      {message.status === OutreachMessageStatus.Queued &&
                        message.channel === OutreachChannel.LinkedIn && (
                          <span className="mt-1 block text-xs text-muted-foreground">
                            Waiting for the local LinkedIn worker
                          </span>
                        )}
                    </TableCell>
                    <TableCell>{formatDateTime(message.sentAt)}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-2">
                        {(message.status === OutreachMessageStatus.Draft ||
                          message.status === OutreachMessageStatus.Failed) && (
                          <Button
                            size="sm"
                            variant="outline"
                            disabled={sendingId !== null}
                            onClick={() => onSend(message.id)}
                          >
                            {sendingId === message.id ? <Loader2 className="animate-spin" /> : <Send />}
                            Send
                          </Button>
                        )}
                        {!message.repliedAt && message.status === OutreachMessageStatus.Sent && (
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => markReplied({ campaignId, messageId: message.id })}
                          >
                            <MessageSquareReply />
                            Mark replied
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
      {profileLead && (
        <LeadProfileModal lead={profileLead} onClose={() => setProfileLead(null)} />
      )}
    </div>
  )
}

function Stat({ icon, label, value }: { icon: ReactNode; label: string; value: number }) {
  return (
    <Card>
      <CardContent className="flex items-center gap-3 p-4">
        <span className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-primary [&_svg]:h-4 [&_svg]:w-4">
          {icon}
        </span>
        <div>
          <p className="text-2xl font-semibold leading-none">{value}</p>
          <p className="text-xs text-muted-foreground">{label}</p>
        </div>
      </CardContent>
    </Card>
  )
}

function TrackingCell({ value }: { value: string | null }) {
  if (!value) {
    return <span className="text-muted-foreground">—</span>
  }
  return (
    <span className="inline-flex items-center gap-1 text-emerald-600" title={formatDateTime(value)}>
      <Check className="h-4 w-4" />
      <span className="text-xs text-muted-foreground">{formatDate(value)}</span>
    </span>
  )
}
