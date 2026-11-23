import { useState } from 'react'
import { BarChart3, Loader2, Users } from 'lucide-react'
import {
  useGetCampaignReportCandidatesQuery,
  useGetCampaignReportQuery,
  useGetCampaignsQuery,
  useGetClientsQuery,
  useGetJobsQuery,
  useGetLookupsQuery,
  useGetTalentFunnelQuery,
} from '@/services/apiSlice'
import type { FunnelCounts, FunnelGroup } from '@/types/models'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { formatDate } from '@/lib/formatDate'

const FUNNEL_STAGES: { key: keyof FunnelCounts; label: string }[] = [
  { key: 'identified', label: 'Identified' },
  { key: 'contacted', label: 'Contacted' },
  { key: 'engaged', label: 'Engaged' },
  { key: 'qualified', label: 'Qualified' },
  { key: 'interviewReady', label: 'Interview ready' },
  { key: 'interviewScheduled', label: 'Interview scheduled' },
  { key: 'interviewHeld', label: 'Interview held' },
]

function FunnelOverall({ counts }: { counts: FunnelCounts }) {
  return (
    <div className="grid gap-3 sm:grid-cols-4 lg:grid-cols-7">
      {FUNNEL_STAGES.map((stage) => (
        <div key={stage.key} className="rounded-md border p-3">
          <p className="text-2xl font-semibold leading-none">{counts[stage.key]}</p>
          <p className="mt-1 text-xs text-muted-foreground">{stage.label}</p>
        </div>
      ))}
    </div>
  )
}

function FunnelGroupTable({ groups, keyLabel }: { groups: FunnelGroup[]; keyLabel: string }) {
  if (groups.length === 0) {
    return <p className="text-sm text-muted-foreground">No data.</p>
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>{keyLabel}</TableHead>
          {FUNNEL_STAGES.map((stage) => (
            <TableHead key={stage.key}>{stage.label}</TableHead>
          ))}
        </TableRow>
      </TableHeader>
      <TableBody>
        {groups.map((group) => (
          <TableRow key={group.key}>
            <TableCell className="font-medium">{group.key}</TableCell>
            {FUNNEL_STAGES.map((stage) => (
              <TableCell key={stage.key}>{group.counts[stage.key]}</TableCell>
            ))}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}

export function ReportsPage() {
  const { data: campaigns = [] } = useGetCampaignsQuery()
  const { data: jobs = [] } = useGetJobsQuery()
  const { data: clients = [] } = useGetClientsQuery()
  const [clientFilter, setClientFilter] = useState<string>('')
  const [selectedCampaignId, setSelectedCampaignId] = useState<string>('')
  const [reportCampaignId, setReportCampaignId] = useState<number | null>(null)
  const [funnelClientId, setFunnelClientId] = useState<string>('')
  const [funnelRoleFamily, setFunnelRoleFamily] = useState<string>('')
  const [funnelIndustry, setFunnelIndustry] = useState<string>('')

  const { data: roleFamilyLookups = [] } = useGetLookupsQuery('role-family')
  const { data: industryLookups = [] } = useGetLookupsQuery('industry')
  const { data: funnel, isFetching: funnelLoading } = useGetTalentFunnelQuery({
    clientId: funnelClientId ? Number(funnelClientId) : undefined,
    roleFamily: funnelRoleFamily || undefined,
    industry: funnelIndustry || undefined,
  })

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

  const jobById = new Map(jobs.map((job) => [job.id, job]))
  const clientNameById = new Map(clients.map((client) => [client.id, client.name]))

  const campaignClientId = (campaign: (typeof campaigns)[number]): string => {
    const job = campaign.jobId ? jobById.get(campaign.jobId) : undefined
    return job?.clientId ? String(job.clientId) : ''
  }

  const visibleCampaigns = clientFilter
    ? campaigns.filter((campaign) => campaignClientId(campaign) === clientFilter)
    : campaigns

  const selectedCampaign = campaigns.find((campaign) => String(campaign.id) === selectedCampaignId)
  const selectedJob = selectedCampaign?.jobId ? jobById.get(selectedCampaign.jobId) : undefined
  const selectedClientName = selectedJob?.clientId
    ? clientNameById.get(selectedJob.clientId)
    : undefined

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
          <div className="flex flex-wrap items-end gap-3">
            <div className="space-y-1.5">
              <Label>Client</Label>
              <Select
                className="w-56"
                value={clientFilter}
                onChange={(e) => {
                  setClientFilter(e.target.value)
                  setSelectedCampaignId('')
                }}
              >
                <option value="">All clients</option>
                {clients.map((client) => (
                  <option key={client.id} value={client.id}>
                    {client.name}
                  </option>
                ))}
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Campaign</Label>
              <Select
                className="w-80"
                value={selectedCampaignId}
                onChange={(e) => setSelectedCampaignId(e.target.value)}
              >
                <option value="">Pick a campaign...</option>
                {visibleCampaigns.map((c) => (
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
          {selectedCampaign && (selectedClientName || selectedJob) && (
            <p className="mt-2 text-xs text-muted-foreground">
              {[selectedClientName, selectedJob?.title].filter(Boolean).join(' · ')}
            </p>
          )}
          {error && <p className="mt-2 text-sm text-destructive">{error}</p>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Talent funnel</CardTitle>
          <CardDescription>
            Candidates by funnel stage, grouped by client, role family and industry. The filters
            apply to all three tables.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap items-end gap-3">
            <div className="space-y-1.5">
              <Label>Client</Label>
              <Select
                className="w-56"
                value={funnelClientId}
                onChange={(e) => setFunnelClientId(e.target.value)}
              >
                <option value="">All clients</option>
                {clients.map((client) => (
                  <option key={client.id} value={client.id}>
                    {client.name}
                  </option>
                ))}
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Role family</Label>
              <Select
                className="w-56"
                value={funnelRoleFamily}
                onChange={(e) => setFunnelRoleFamily(e.target.value)}
              >
                <option value="">All role families</option>
                {roleFamilyLookups.map((lookup) => (
                  <option key={lookup.id} value={lookup.value}>
                    {lookup.value}
                  </option>
                ))}
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label>Industry</Label>
              <Select
                className="w-56"
                value={funnelIndustry}
                onChange={(e) => setFunnelIndustry(e.target.value)}
              >
                <option value="">All industries</option>
                {industryLookups.map((lookup) => (
                  <option key={lookup.id} value={lookup.value}>
                    {lookup.value}
                  </option>
                ))}
              </Select>
            </div>
          </div>

          {funnelLoading ? (
            <p className="flex items-center gap-2 text-sm text-muted-foreground">
              <Loader2 className="animate-spin" /> Loading funnel...
            </p>
          ) : funnel ? (
            <>
              <FunnelOverall counts={funnel.overall} />
              <div>
                <p className="mb-2 text-sm font-medium">By client</p>
                <FunnelGroupTable groups={funnel.byClient} keyLabel="Client" />
              </div>
              <div>
                <p className="mb-2 text-sm font-medium">By role family</p>
                <FunnelGroupTable groups={funnel.byRoleFamily} keyLabel="Role family" />
              </div>
              <div>
                <p className="mb-2 text-sm font-medium">By industry</p>
                <FunnelGroupTable groups={funnel.byIndustry} keyLabel="Industry" />
              </div>
            </>
          ) : (
            <p className="text-sm text-muted-foreground">No funnel data yet.</p>
          )}
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
