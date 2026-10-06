import { Loader2, Video } from 'lucide-react'
import { useGetUpcomingInterviewsQuery } from '@/services/apiSlice'
import { formatDateTime } from '@/lib/formatDate'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

export function InterviewsPage() {
  const { data: interviews = [], isLoading } = useGetUpcomingInterviewsQuery()

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold">Interviews</h2>
        <p className="text-sm text-muted-foreground">
          Teams interviews with candidates. Past interviews are marked completed automatically.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Upcoming and recent ({interviews.length})</CardTitle>
          <CardDescription>Each interview has a Teams join link and is on your calendar.</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="animate-spin" /> Loading…
            </p>
          ) : interviews.length === 0 ? (
            <p className="text-sm text-muted-foreground">No upcoming interviews.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>When</TableHead>
                  <TableHead>Candidate</TableHead>
                  <TableHead>Length</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Join</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {interviews.map((interview) => (
                  <TableRow key={interview.id}>
                    <TableCell className="font-medium">{formatDateTime(interview.startAt)}</TableCell>
                    <TableCell>{interview.candidateName ?? `#${interview.leadId}`}</TableCell>
                    <TableCell>{interview.durationMinutes} min</TableCell>
                    <TableCell>
                      <Badge
                        variant={
                          interview.status === 'NoShow'
                            ? 'destructive'
                            : interview.status === 'Completed'
                              ? 'secondary'
                              : interview.status === 'Booked'
                                ? 'success'
                                : 'outline'
                        }
                      >
                        {interview.status}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      {interview.teamsJoinUrl ? (
                        <a
                          href={interview.teamsJoinUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="inline-flex items-center gap-1 text-sm text-blue-600 hover:underline"
                        >
                          <Video className="h-4 w-4" /> Join
                        </a>
                      ) : (
                        '—'
                      )}
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
