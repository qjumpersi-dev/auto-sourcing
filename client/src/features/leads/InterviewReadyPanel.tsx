import { useEffect, useState } from 'react'
import { Check, ClipboardCheck, Loader2, Save, Send } from 'lucide-react'
import {
  useConfirmQualificationCheckMutation,
  useGetQualificationQuery,
  useLazyGetQualificationHandoverQuery,
  useMarkHandoverSentMutation,
  useUpdateQualificationCheckMutation,
  useUpdateQualificationMutation,
} from '@/services/apiSlice'
import {
  InterviewReadyStatus,
  interviewReadyStatusLabels,
  type QualificationCheck,
} from '@/types/models'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { formatDateTime } from '@/lib/formatDate'

function statusToNumber(value: string): number {
  switch (value) {
    case 'Met':
      return InterviewReadyStatus.Met
    case 'NotMet':
      return InterviewReadyStatus.NotMet
    case 'NotApplicable':
      return InterviewReadyStatus.NotApplicable
    default:
      return InterviewReadyStatus.Unknown
  }
}

function CheckRow({ check, leadId }: { check: QualificationCheck; leadId: number }) {
  const [status, setStatus] = useState(statusToNumber(check.status))
  const [answer, setAnswer] = useState(check.answer ?? '')
  const [notes, setNotes] = useState(check.notes ?? '')
  const [error, setError] = useState<string | null>(null)
  const [updateCheck, { isLoading: saving }] = useUpdateQualificationCheckMutation()
  const [confirmCheck, { isLoading: confirming }] = useConfirmQualificationCheckMutation()

  useEffect(() => {
    setStatus(statusToNumber(check.status))
    setAnswer(check.answer ?? '')
    setNotes(check.notes ?? '')
  }, [check.updatedAt, check.status, check.answer, check.notes])

  const onSave = async () => {
    setError(null)
    try {
      await updateCheck({
        checkId: check.id,
        leadId,
        status,
        answer: answer || null,
        notes: notes || null,
        source: 1,
      }).unwrap()
    } catch {
      setError('Could not save.')
    }
  }

  const onConfirm = async () => {
    setError(null)
    try {
      await confirmCheck({ checkId: check.id, leadId }).unwrap()
    } catch {
      setError('Could not confirm.')
    }
  }

  return (
    <div className="rounded-md border p-3">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="text-sm font-medium">{check.criterionLabel}</p>
          <p className="text-xs text-muted-foreground">{check.prompt}</p>
          {check.expected && (
            <p className="mt-1 text-xs">
              <span className="font-medium">Expected:</span> {check.expected}
            </p>
          )}
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={check.source === 'Human' ? 'success' : 'secondary'}>{check.source}</Badge>
          {check.confirmedAt && (
            <span className="text-xs text-muted-foreground">{formatDateTime(check.confirmedAt)}</span>
          )}
        </div>
      </div>

      <div className="mt-2 grid gap-2 sm:grid-cols-[170px_1fr]">
        <Select value={String(status)} onChange={(e) => setStatus(Number(e.target.value))}>
          {Object.entries(interviewReadyStatusLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </Select>
        <Input
          value={answer}
          onChange={(e) => setAnswer(e.target.value)}
          placeholder="Candidate's answer"
        />
      </div>

      <Textarea
        className="mt-2"
        rows={2}
        value={notes}
        onChange={(e) => setNotes(e.target.value)}
        placeholder="Notes (optional)"
      />

      {error && <p className="mt-1 text-xs text-destructive">{error}</p>}

      <div className="mt-2 flex gap-2">
        <Button type="button" size="sm" variant="outline" onClick={onSave} disabled={saving}>
          {saving ? <Loader2 className="animate-spin" /> : <Save />}
          Save
        </Button>
        <Button type="button" size="sm" variant="outline" onClick={onConfirm} disabled={confirming}>
          {confirming ? <Loader2 className="animate-spin" /> : <Check />}
          Confirm
        </Button>
      </div>
    </div>
  )
}

export function InterviewReadyPanel({
  leadId,
  campaignId,
}: {
  leadId: number
  campaignId?: number | null
}) {
  const { data, isLoading } = useGetQualificationQuery({ leadId, campaignId: campaignId ?? null })
  const [updateHeader, { isLoading: savingHeader }] = useUpdateQualificationMutation()
  const [loadHandover, { data: handover, isFetching: loadingHandover }] = useLazyGetQualificationHandoverQuery()
  const [markSent, { isLoading: markingSent }] = useMarkHandoverSentMutation()

  const [payExpectation, setPayExpectation] = useState('')
  const [availability, setAvailability] = useState('')
  const [noticePeriod, setNoticePeriod] = useState('')
  const [initialised, setInitialised] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (data && !initialised) {
      setPayExpectation(data.payExpectation ?? '')
      setAvailability(data.availability ?? '')
      setNoticePeriod(data.noticePeriod ?? '')
      setInitialised(true)
    }
  }, [data, initialised])

  if (isLoading || !data) {
    return (
      <p className="flex items-center gap-2 text-sm text-muted-foreground">
        <Loader2 className="animate-spin" /> Loading checklist...
      </p>
    )
  }

  const onSaveHeader = async () => {
    setError(null)
    try {
      await updateHeader({
        id: data.id,
        leadId,
        payExpectation: payExpectation || null,
        availability: availability || null,
        noticePeriod: noticePeriod || null,
      }).unwrap()
    } catch {
      setError('Could not save the candidate details.')
    }
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant={data.agentComplete ? 'success' : 'secondary'}>
          {data.agentComplete ? 'Agent checks complete' : 'Agent checks pending'}
        </Badge>
        <Badge variant={data.interviewReady ? 'success' : 'outline'}>
          {data.interviewReady ? 'Interview ready' : 'Not yet interview ready'}
        </Badge>
        {data.handoverSentAt && (
          <span className="text-xs text-muted-foreground">
            Handover sent {formatDateTime(data.handoverSentAt)}
            {data.handoverSentTo ? ` to ${data.handoverSentTo}` : ''}
          </span>
        )}
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div className="space-y-1.5">
          <Label>Pay expectation</Label>
          <Input value={payExpectation} onChange={(e) => setPayExpectation(e.target.value)} placeholder="e.g. USD 95,000" />
        </div>
        <div className="space-y-1.5">
          <Label>Availability</Label>
          <Input value={availability} onChange={(e) => setAvailability(e.target.value)} placeholder="e.g. immediately" />
        </div>
        <div className="space-y-1.5">
          <Label>Notice period</Label>
          <Input value={noticePeriod} onChange={(e) => setNoticePeriod(e.target.value)} placeholder="e.g. 4 weeks" />
        </div>
      </div>
      <Button type="button" size="sm" variant="outline" onClick={onSaveHeader} disabled={savingHeader}>
        {savingHeader ? <Loader2 className="animate-spin" /> : <Save />}
        Save details
      </Button>
      {error && <p className="text-xs text-destructive">{error}</p>}

      <div className="space-y-2">
        {data.checks.map((check) => (
          <CheckRow key={check.id} check={check} leadId={leadId} />
        ))}
      </div>

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => loadHandover(data.id)}
          disabled={loadingHandover}
        >
          {loadingHandover ? <Loader2 className="animate-spin" /> : <ClipboardCheck />}
          Build handover
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => void markSent({ id: data.id, leadId })}
          disabled={markingSent}
        >
          {markingSent ? <Loader2 className="animate-spin" /> : <Send />}
          Mark sent to HR Assist
        </Button>
      </div>

      {handover?.html && (
        <div
          className="rounded-md border bg-muted/30 p-3 text-sm [&_h3]:mb-1 [&_h3]:font-semibold [&_li]:ml-4 [&_li]:list-disc [&_p]:mb-1"
          dangerouslySetInnerHTML={{ __html: handover.html }}
        />
      )}
    </div>
  )
}