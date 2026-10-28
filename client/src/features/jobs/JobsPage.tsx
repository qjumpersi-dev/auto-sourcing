import { useState } from 'react'
import { Loader2, Plus, Trash2 } from 'lucide-react'
import {
  useDeleteJobMutation,
  useGetClientsQuery,
  useGetJobsQuery,
} from '@/services/apiSlice'
import { jobStatusLabels, JobStatus } from '@/types/models'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'

export function JobsPage({
  onNewJob,
  onOpenJob,
}: {
  onNewJob: () => void
  onOpenJob: (id: number) => void
}) {
  const { data: jobs = [], isLoading } = useGetJobsQuery()
  const { data: clients = [] } = useGetClientsQuery()
  const [deleteJob] = useDeleteJobMutation()
  const [error, setError] = useState<string | null>(null)
  const [clientFilter, setClientFilter] = useState('')

  const clientNameById = new Map(clients.map((client) => [client.id, client.name]))
  const visibleJobs = clientFilter
    ? jobs.filter((job) => String(job.clientId ?? '') === clientFilter)
    : jobs

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
          <div className="flex flex-wrap items-end justify-between gap-3">
            <div>
              <CardTitle>Your jobs ({visibleJobs.length})</CardTitle>
              <CardDescription>Click a job to view or edit its details.</CardDescription>
            </div>
            <div className="space-y-1.5">
              <Label>Filter by client</Label>
              <Select
                className="w-64"
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
            <p className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="animate-spin" /> Loading...
            </p>
          ) : visibleJobs.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              {jobs.length === 0
                ? 'No jobs yet. Create one to get started.'
                : 'No jobs for this client.'}
            </p>
          ) : (
            <ul className="divide-y">
              {visibleJobs.map((job) => (
                <li key={job.id}>
                  <div className="flex items-center justify-between gap-3 rounded-lg px-3 py-3 transition-colors hover:bg-accent">
                    <button
                      type="button"
                      onClick={() => onOpenJob(job.id)}
                      className="flex-1 text-left"
                    >
                      <p className="font-medium">{job.title}</p>
                      <p className="text-sm text-muted-foreground">
                        {[
                          clientNameById.get(job.clientId ?? -1),
                          job.location || 'No location',
                          job.roleFamily,
                          job.industry,
                        ]
                          .filter(Boolean)
                          .join(' · ')}
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
