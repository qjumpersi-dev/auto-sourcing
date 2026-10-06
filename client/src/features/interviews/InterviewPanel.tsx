import { useState } from 'react'
import { CalendarPlus, Loader2, Video } from 'lucide-react'
import {
  useBookInterviewMutation,
  useCancelInterviewMutation,
  useGetLeadInterviewsQuery,
  useLazyGetInterviewSlotsQuery,
} from '@/services/apiSlice'
import { formatDateTime } from '@/lib/formatDate'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'

export function InterviewPanel({ leadId, campaignId }: { leadId: number; campaignId?: number | null }) {
  const { data: interviews = [] } = useGetLeadInterviewsQuery(leadId)
  const [fetchSlots, { isFetching: loadingSlots }] = useLazyGetInterviewSlotsQuery()
  const [bookInterview, { isLoading: booking }] = useBookInterviewMutation()
  const [cancelInterview] = useCancelInterviewMutation()

  const [slots, setSlots] = useState<{ startUtc: string; label: string }[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [info, setInfo] = useState<string | null>(null)

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
      await bookInterview({ leadId, startAt: startUtc, campaignId: campaignId ?? null }).unwrap()
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

  const active = interviews.filter((i) => i.status === 'Booked')

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
            <Badge variant="success">Booked</Badge>
          </div>
          <div className="mt-2 flex items-center gap-2">
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
            <Button size="sm" variant="ghost" onClick={() => onCancel(interview.id)}>
              Cancel
            </Button>
          </div>
        </div>
      ))}

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
