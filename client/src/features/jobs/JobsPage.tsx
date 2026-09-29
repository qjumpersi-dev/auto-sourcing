import { useState } from 'react'
import { Loader2, Plus, Trash2 } from 'lucide-react'
import {
  useDeleteJobMutation,
  useGetJobsQuery,
} from '@/services/apiSlice'
import { jobStatusLabels, JobStatus } from '@/types/models'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function JobsPage({
  onNewJob,
  onOpenJob,
}: {
  onNewJob: () => void
  onOpenJob: (id: number) => void
}) {
  const { data: jobs = [], isLoading } = useGetJobsQuery()
  const [deleteJob] = useDeleteJobMutation()
  const [error, setError] = useState<string | null>(null)

  const onDelete = async (id: number, title: string) => {
    if (!window.confirm(`Delete the job "${title}"? This cannot be undone.`)) return
    setError(null)
    try {
      await deleteJob(id).unwrap()
    } catch {
      setError('Could not delete the job.')
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold">Jobs</h2>
          <p className="text-sm text-muted-foreground">
            Manage job listings for AI agent engagement and candidate outreach.
          </p>
        </div>
        <Button onClick={onNewJob}>
          <Plus />
          New job
        </Button>
      </div>

      {error && (
        <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Your jobs ({jobs.length})</CardTitle>
          <CardDescription>Click a job to view or edit its details.</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="animate-spin" /> Loading...
            </p>
          ) : jobs.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No jobs yet. Create one to get started.
            </p>
          ) : (
            <ul className="divide-y">
              {jobs.map((job) => (
                <li key={job.id}>
                  <div className="flex items-center justify-between gap-3 rounded-lg px-3 py-3 transition-colors hover:bg-accent">
                    <button
                      type="button"
                      onClick={() => onOpenJob(job.id)}
                      className="flex-1 text-left"
                    >
                      <p className="font-medium">{job.title}</p>
                      <p className="text-sm text-muted-foreground">
                        {job.location || 'No location'}
                        {job.industry ? ` · ${job.industry}` : ''}
                      </p>
                    </button>
                    <Badge
                      variant={job.status === JobStatus.Active ? 'success' : 'secondary'}
                    >
                      {jobStatusLabels[job.status] ?? 'Unknown'}
                    </Badge>
                    <Button
                      variant="ghost"
                      size="icon"
                      aria-label={`Delete ${job.title}`}
                      onClick={() => onDelete(job.id, job.title)}
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
