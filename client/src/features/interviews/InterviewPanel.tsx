import { useState } from 'react'
import { CalendarPlus, Check, FileText, Loader2, Sparkles, Video } from 'lucide-react'
import {
  useBookInterviewMutation,
  useCancelInterviewMutation,
  useGetJobsQuery,
  useGetLeadInterviewsQuery,
  useLazyGetInterviewSlotsQuery,
  useProcessInterviewTranscriptMutation,
  useUpdateInterviewRoleMutation,
} from '@/services/apiSlice'
import { formatDateTime } from '@/lib/formatDate'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'

export function InterviewPanel({ leadId, campaignId }: { leadId: number; campaignId?: number | null }) {
  const { data: interviews = [] } = useGetLeadInterviewsQuery(leadId)
  const { data: jobs = [] } = useGetJobsQuery()
  const [fetchSlots, { isFetching: loadingSlots }] = useLazyGetInterviewSlotsQuery()
  const [bookInterview, { isLoading: booking }] = useBookInterviewMutation()
  const [cancelInterview] = useCancelInterviewMutation()
  const [processTranscript, { isLoading: processing }] = useProcessInterviewTranscriptMutation()

  const [slots, setSlots] = useState<{ startUtc: string; label: string }[] | null>(null)
  const [jobId, setJobId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [info, setInfo] = useState<string | null>(null)
  const [roleSavedFor, setRoleSavedFor] = useState<number | null>(null)
  const [updateRole, { isLoading: savingRole }] = useUpdateInterviewRoleMutation()

  const onFindTimes = async () => {
    setError(null)
    setInfo(null)
    setSlots(null)
    try {
      const result = await fetchSlots(leadId).unwrap()
      if (result.length === 0) {
        setError("No free times in the next week — check the organiser's Microsoft 365 is connected.")
      } else {
        setSlots(result)
      }
    } catch {
      setError('Could not load available times.')
    }
  }

  const onBook = async (startUtc: string) => {
    setError(null)
    setInfo(null)
    try {
      await bookInterview({
        leadId,
        startAt: startUtc,
        campaignId: campaignId ?? null,
        jobId: jobId ? Number(jobId) : null,
      }).unwrap()
      setSlots(null)
      setInfo('Interview booked — the Teams invitation has been sent.')
    } catch (err) {
      const data = (err as { data?: { error?: string } })?.data
      setError(data?.error ?? 'Could not book the interview.')
    }
  }

  const onCancel = async (id: number) => {
    setError(null)
    setInfo(null)
    try {
      await cancelInterview({ id }).unwrap()
    } catch {
      setError('Could not cancel the interview.')
    }
  }

  const onProcess = async (id: number) => {
    setError(null)
    setInfo(null)
    try {
      const result = await processTranscript(id).unwrap()
      setInfo(
        result.processed
          ? 'Transcript pulled and the AI summary saved to the candidate notes.'
          : 'No transcript available yet. Make sure the meeting was recorded/transcribed, then try again.',
      )
    } catch {
      setError('Could not process the transcript.')
    }
  }

  const active = interviews.filter((i) => i.status === 'Booked' || i.status === 'Completed')

  return (
    <div className="space-y-3">
      {active.length === 0 && <p className="text-sm text-muted-foreground">No interview booked yet.</p>}

      {active.map((interview) => (
        <div key={interview.id} className="rounded-md border p-3">
          <div className="flex items-center justify-between gap-3">
            <div>
              <p className="text-sm font-medium">{formatDateTime(interview.startAt)}</p>
              <p className="text-xs text-muted-foreground">{interview.durationMinutes} minutes · Teams</p>
            </div>
            <Badge variant={interview.status === 'Completed' ? 'secondary' : 'success'}>{interview.status}</Badge>
          </div>

          <div className="mt-2 flex flex-wrap items-center gap-2">
            {interview.teamsJoinUrl && (
              <a
                href={interview.teamsJoinUrl}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-1 text-sm text-blue-600 hover:underline"
              >
                <Video className="h-4 w-4" /> Join link
              </a>
            )}
            <Button size="sm" variant="ghost" onClick={() => onProcess(interview.id)} disabled={processing}>
              {processing ? <Loader2 className="animate-spin" /> : <FileText className="h-4 w-4" />}
              Get transcript &amp; summary
            </Button>
            <Button size="sm" variant="ghost" onClick={() => onCancel(interview.id)}>
              Cancel
            </Button>
          </div>

          {interview.summary && (
            <div className="mt-3 rounded-md bg-muted/40 p-3 text-sm">
              <p className="mb-1 flex items-center gap-1 font-medium">
                <Sparkles className="h-4 w-4 text-blue-600" /> AI interview summary
              </p>
              <div className="[&_h3]:mb-1 [&_h3]:text-sm [&_li]:ml-4 [&_li]:list-disc [&_p]:mb-2" dangerouslySetInnerHTML={{ __html: interview.summary }} />
            </div>
          )}

          <div className="mt-3 flex flex-wrap items-center gap-2">
            <Label className="text-xs text-muted-foreground">Role this interview is for</Label>
            <Select
              className="h-8 w-full max-w-xs"
              value={interview.jobId ?? ''}
              disabled={savingRole}
              onChange={async (e) => {
                const selected = e.target.value
                const next = selected === '' ? null : Number(selected)
                try {
                  await updateRole({ id: interview.id, jobId: next }).unwrap()
                  setRoleSavedFor(interview.id)
                } catch {
                  setError('Could not save the role.')
                }
              }}
            >
              <option value="">Not linked to a role</option>
              {jobs.map((job) => (
                <option key={job.id} value={job.id}>
                  {job.title}
                </option>
              ))}
            </Select>
            {savingRole ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
            ) : (
              roleSavedFor === interview.id && <Check className="h-3.5 w-3.5 text-emerald-600" />
            )}
          </div>
        </div>
      ))}

      <div className="space-y-1.5">
        <Label className="text-xs text-muted-foreground">Role for the new interview (optional)</Label>
        <Select value={jobId} onChange={(e) => setJobId(e.target.value)}>
          <option value="">Not linked to a role</option>
          {jobs.map((job) => (
            <option key={job.id} value={job.id}>
              {job.title}
            </option>
          ))}
        </Select>
      </div>

      <div>
        <Button size="sm" variant="outline" onClick={onFindTimes} disabled={loadingSlots || booking}>
          {loadingSlots ? <Loader2 className="animate-spin" /> : <CalendarPlus />}
          Find available times
        </Button>
      </div>

      {slots && slots.length > 0 && (
        <div className="space-y-2 rounded-md border p-3">
          <p className="text-sm font-medium">Pick a time</p>
          {slots.map((slot) => (
            <Button
              key={slot.startUtc}
              size="sm"
              variant="secondary"
              className="w-full justify-start"
              disabled={booking}
              onClick={() => onBook(slot.startUtc)}
            >
              {slot.label}
            </Button>
          ))}
        </div>
      )}

      {error && <p className="text-sm text-destructive">{error}</p>}
      {info && <p className="text-sm text-emerald-600">{info}</p>}
    </div>
  )
}
