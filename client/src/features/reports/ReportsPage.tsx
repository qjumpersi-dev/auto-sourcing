import { useState } from 'react'
import { BarChart3, Loader2, Users } from 'lucide-react'
import {
  useGetCampaignReportCandidatesQuery,
  useGetCampaignReportQuery,
  useGetCampaignsQuery,
} from '@/services/apiSlice'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { formatDate } from '@/lib/formatDate'

export function ReportsPage() {
  const { data: campaigns = [] } = useGetCampaignsQuery()
  const [selectedCampaignId, setSelectedCampaignId] = useState<string>('')
  const [reportCampaignId, setReportCampaignId] = useState<number | null>(null)

  const {
    data: summary,
    isFetching: summaryLoading,
    isError: summaryError,
  } = useGetCampaignReportQuery(reportCampaignId ?? 0, { skip: reportCampaignId === null })
  const {
    data: candidatesReport,
    isFetching: candidatesLoading,
    isError: candidatesError,
  } = useGetCampaignReportCandidatesQuery(reportCampaignId ?? 0, { skip: reportCampaignId === null })

  const loading = summaryLoading || candidatesLoading
  const error = summaryError || candidatesError ? 'Could not load the report.' : null
  const candidates = candidatesReport?.candidates ?? []

  const loadReport = () => {
    if (!selectedCampaignId) return
    setReportCampaignId(Number(selectedCampaignId))
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold">Reports</h2>
        <p className="text-sm text-muted-foreground">
          Campaign and candidate level reporting for outreach performance.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Select a campaign</CardTitle>
          <CardDescription>Choose a campaign to view its report.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-end gap-3">
            <div className="space-y-1.5">
              <Label>Campaign</Label>
              <Select
                className="w-80"
                value={selectedCampaignId}
                onChange={(e) => setSelectedCampaignId(e.target.value)}
              >
                <option value="">Pick a campaign...</option>
                {campaigns.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </Select>
            </div>
            <Button onClick={loadReport} disabled={loading || !selectedCampaignId}>
              {loading ? <Loader2 className="animate-spin" /> : <BarChart3 />}
              Load report
            </Button>
          </div>
          {error && <p className="mt-2 text-sm text-destructive">{error}</p>}
        </CardContent>
      </Card>

      {summary && (
        <>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Card>
              <CardContent className="flex items-center gap-3 p-4">
                <span className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-primary [&_svg]:h-4 [&_svg]:w-4">
                  <Users />
                </span>
                <div>
                  <p className="text-2xl font-semibold leading-none">{summary.totalCandidates}</p>
                  <p className="text-xs text-muted-foreground">Candidates</p>
                </div>
              </CardContent>
            </Card>
            {summary.steps.slice(0, 3).map((step) => (
              <Card key={step.stepNumber}>
                <CardContent className="flex items-center gap-3 p-4">
                  <span className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-primary [&_svg]:h-4 [&_svg]:w-4">
                    <BarChart3 />
                  </span>
                  <div>
                    <p className="text-2xl font-semibold leading-none">{step.sent}</p>
                    <p className="text-xs text-muted-foreground">Step {step.stepNumber} sent</p>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          {summary.steps.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Step breakdown</CardTitle>
                <CardDescription>Performance per sequence step.</CardDescription>
              </CardHeader>
              <CardContent>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Step</TableHead>
                      <TableHead>Channel</TableHead>
                      <TableHead>Sent</TableHead>
                      <TableHead>Opened</TableHead>
                      <TableHead>Clicked</TableHead>
                      <TableHead>Replied</TableHead>
                      <TableHead>Failed</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {summary.steps.map((step) => (
                      <TableRow key={step.stepNumber}>
                        <TableCell className="font-medium">
                          {step.stepNumber}. {step.stepName}
                        </TableCell>
                        <TableCell>{step.channel}</TableCell>
                        <TableCell>{step.sent}</TableCell>
                        <TableCell>{step.opened}</TableCell>
                        <TableCell>{step.clicked}</TableCell>
                        <TableCell>{step.replied}</TableCell>
                        <TableCell>{step.failed}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          )}
        </>
      )}

      {candidates.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Candidate report ({candidates.length})</CardTitle>
            <CardDescription>Detailed per-candidate outreach status.</CardDescription>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Candidate</TableHead>
                  <TableHead>Current Stage</TableHead>
                  <TableHead>Days</TableHead>
                  <TableHead>Engagement</TableHead>
                  <TableHead>Last Contact</TableHead>
                  <TableHead>Last Reply</TableHead>
                  <TableHead>Next Action</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>SMS</TableHead>
                  <TableHead>Flags</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {candidates.map((row) => (
                  <TableRow key={row.leadId}>
                    <TableCell className="font-medium">{row.candidateName}</TableCell>
                    <TableCell>{row.currentStage}</TableCell>
                    <TableCell>{row.daysInStage}</TableCell>
                    <TableCell>
                      <Badge
                        variant={
                          row.engagementStatus === 'Engaged'
                            ? 'success'
                            : row.engagementStatus === 'Waiting'
                              ? 'warning'
                              : 'secondary'
                        }
                      >
                        {row.engagementStatus}
                      </Badge>
                    </TableCell>
                    <TableCell>{formatDate(row.lastContact)}</TableCell>
                    <TableCell>{formatDate(row.lastReply)}</TableCell>
                    <TableCell>{row.nextAction}</TableCell>
                    <TableCell>{row.emailStatus}</TableCell>
                    <TableCell>{row.smsStatus}</TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        {row.optOut && <Badge variant="destructive">Opt out</Badge>}
                        {row.humanAttention && <Badge variant="warning">Attention</Badge>}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
