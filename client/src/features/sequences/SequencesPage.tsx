import { useState } from 'react'
import { CalendarClock, Loader2, Plus, Trash2 } from 'lucide-react'
import {
  useDeleteSequenceMutation,
  useGetSequencesQuery,
} from '@/services/apiSlice'
import { sequenceStatusLabels, SequenceStatus } from '@/types/models'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { formatSendDays, formatTimeWindow } from './sequenceUtils'

export function SequencesPage({
  onNewSequence,
  onOpenSequence,
}: {
  onNewSequence: () => void
  onOpenSequence: (id: number) => void
}) {
  const { data: sequences = [], isLoading } = useGetSequencesQuery()
  const [deleteSequence] = useDeleteSequenceMutation()
  const [error, setError] = useState<string | null>(null)

  const onDelete = async (id: number, name: string) => {
    if (!window.confirm(`Delete the sequence "${name}"? This cannot be undone.`)) return
    setError(null)
    try {
      await deleteSequence(id).unwrap()
    } catch {
      setError('Could not delete the sequence.')
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold">Sequences</h2>
          <p className="text-sm text-muted-foreground">
            Build multi-step outreach sequences and choose when each follow up is sent.
          </p>
        </div>
        <Button onClick={onNewSequence}>
          <Plus />
          New sequence
        </Button>
      </div>

      {error && (
        <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Your sequences ({sequences.length})</CardTitle>
          <CardDescription>Click a sequence to manage its steps and schedule.</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="animate-spin" /> Loading...
            </p>
          ) : sequences.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No sequences yet. Create one to get started.
            </p>
          ) : (
            <ul className="divide-y">
              {sequences.map((sequence) => (
                <li key={sequence.id}>
                  <div className="flex items-center justify-between gap-3 rounded-lg px-3 py-3 transition-colors hover:bg-accent">
                    <button
                      type="button"
                      onClick={() => onOpenSequence(sequence.id)}
                      className="flex-1 text-left"
                    >
                      <p className="font-medium">{sequence.name}</p>
                      <p className="text-sm text-muted-foreground">
                        {sequence.steps.length} step{sequence.steps.length === 1 ? '' : 's'}
                        <span className="mx-1.5">·</span>
                        <CalendarClock className="mr-1 inline h-3.5 w-3.5 align-text-bottom" />
                        {formatSendDays(sequence.sendDaysMask)} at{' '}
                        {formatTimeWindow(sequence.sendWindowStart, sequence.sendWindowEnd)}
                      </p>
                    </button>
                    <Badge
                      variant={sequence.status === SequenceStatus.Active ? 'success' : 'secondary'}
                    >
                      {sequenceStatusLabels[sequence.status] ?? 'Unknown'}
                    </Badge>
                    <Button
                      variant="ghost"
                      size="icon"
                      aria-label={`Delete ${sequence.name}`}
                      onClick={() => onDelete(sequence.id, sequence.name)}
                    >
                      <Trash2 className="text-destructive" />
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
