import { useState } from 'react'
import { ArrowDown, ArrowUp, ChevronLeft, ChevronRight, ChevronsUpDown, Loader2, MailPlus } from 'lucide-react'
import {
  useAddLeadsToCampaignMutation,
  useGetCampaignsQuery,
  useGetLeadsQuery,
  useUpdateLeadStatusMutation,
} from '@/services/apiSlice'
import { leadStatusLabels } from '@/types/models'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

const PAGE_SIZE = 100

type SortKey = 'name' | 'email' | 'company' | 'jobtitle' | 'status' | 'dateadded' | 'campaigns'

function formatDate(value: string | null | undefined): string {
  if (!value) return '-'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return '-'
  return d.toLocaleDateString()
}

export function LeadsPage() {
  const [leadPage, setLeadPage] = useState(1)
  const [filterCampaignId, setFilterCampaignId] = useState('')
  const [sortBy, setSortBy] = useState<SortKey>('dateadded')
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc')
  const [addedFrom, setAddedFrom] = useState('')
  const [addedTo, setAddedTo] = useState('')
  const { data: leadsData, isLoading: leadsLoading } = useGetLeadsQuery({
    page: leadPage,
    pageSize: PAGE_SIZE,
    campaignId: filterCampaignId ? Number(filterCampaignId) : undefined,
    sortBy,
    sortOrder,
    addedFrom: addedFrom || undefined,
    addedTo: addedTo || undefined,
  })
  const leads = leadsData?.items ?? []
  const totalLeads = leadsData?.totalCount ?? 0
  const totalPages = leadsData?.totalPages ?? 0
  const [updateLeadStatus] = useUpdateLeadStatusMutation()
  const [addLeadsToCampaign, { isLoading: adding }] = useAddLeadsToCampaignMutation()
  const { data: campaigns = [] } = useGetCampaignsQuery()

  const [selectedLeadIds, setSelectedLeadIds] = useState<Set<number>>(new Set())
  const [targetCampaign, setTargetCampaign] = useState('')
  const [addFeedback, setAddFeedback] = useState<string | null>(null)

  const onFilterChange = (value: string) => {
    setFilterCampaignId(value)
    setLeadPage(1)
    setSelectedLeadIds(new Set())
  }

  const onSort = (key: SortKey) => {
    if (sortBy === key) {
      setSortOrder((o) => (o === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortBy(key)
      setSortOrder('asc')
    }
    setLeadPage(1)
  }

  const toggleLead = (id: number) => {
    setSelectedLeadIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }

  const allLeadsSelected = leads.length > 0 && leads.every((l) => selectedLeadIds.has(l.id))

  const toggleAllLeads = () => {
    setSelectedLeadIds(allLeadsSelected ? new Set() : new Set(leads.map((l) => l.id)))
  }

  const onAddToCampaign = async () => {
    if (!targetCampaign || selectedLeadIds.size === 0) return
    setAddFeedback(null)
    try {
      const result = await addLeadsToCampaign({
        campaignId: Number(targetCampaign),
        leadIds: [...selectedLeadIds],
      }).unwrap()
      setAddFeedback(
        `Added ${result.added} lead(s) to the campaign.${result.skipped > 0 ? ` ${result.skipped} skipped (already in campaign or opted out).` : ''}`,
      )
      setSelectedLeadIds(new Set())
    } catch {
      setAddFeedback('Could not add leads. Does the campaign have templates set?')
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Your leads ({totalLeads})</CardTitle>
        <CardDescription>Imported contacts and their outreach status.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex flex-wrap items-center gap-2 rounded-md border bg-muted/30 p-3">
          <span className="text-sm text-muted-foreground">{selectedLeadIds.size} selected</span>
          <Select
            className="h-8 w-56 text-xs"
            value={targetCampaign}
            onChange={(e) => setTargetCampaign(e.target.value)}
          >
            <option value="">Pick a campaign...</option>
            {campaigns.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </Select>
          <Button
            size="sm"
            disabled={adding || selectedLeadIds.size === 0 || !targetCampaign}
            onClick={onAddToCampaign}
          >
            {adding ? <Loader2 className="animate-spin" /> : <MailPlus />}
            Add to campaign
          </Button>
          {addFeedback && <span className="text-xs text-muted-foreground">{addFeedback}</span>}
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Label className="text-xs text-muted-foreground">Filter by campaign</Label>
          <Select
            className="h-8 w-56 text-xs"
            value={filterCampaignId}
            onChange={(e) => onFilterChange(e.target.value)}
          >
            <option value="">All campaigns</option>
            {campaigns.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </Select>
          <Label className="text-xs text-muted-foreground">Date added</Label>
          <Input
            type="date"
            className="h-8 w-40 text-xs"
            value={addedFrom}
            onChange={(e) => {
              setAddedFrom(e.target.value)
              setLeadPage(1)
            }}
          />
          <span className="text-xs text-muted-foreground">to</span>
          <Input
            type="date"
            className="h-8 w-40 text-xs"
            value={addedTo}
            onChange={(e) => {
              setAddedTo(e.target.value)
              setLeadPage(1)
            }}
          />
        </div>

        {leadsLoading ? (
          <p className="text-sm text-muted-foreground">Loading...</p>
        ) : leads.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No leads yet - search candidates and save them first.
          </p>
        ) : (
          <>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10">
                    <input
                      type="checkbox"
                      aria-label="Select all leads"
                      checked={allLeadsSelected}
                      onChange={toggleAllLeads}
                    />
                  </TableHead>
                  <SortableHeader label="Name" sortKey="name" sortBy={sortBy} sortOrder={sortOrder} onSort={onSort} />
                  <SortableHeader label="Email" sortKey="email" sortBy={sortBy} sortOrder={sortOrder} onSort={onSort} />
                  <SortableHeader label="Company" sortKey="company" sortBy={sortBy} sortOrder={sortOrder} onSort={onSort} />
                  <SortableHeader label="Job title" sortKey="jobtitle" sortBy={sortBy} sortOrder={sortOrder} onSort={onSort} />
                  <SortableHeader label="Status" sortKey="status" sortBy={sortBy} sortOrder={sortOrder} onSort={onSort} />
                  <SortableHeader label="Campaigns" sortKey="campaigns" sortBy={sortBy} sortOrder={sortOrder} onSort={onSort} />
                  <SortableHeader label="Date added" sortKey="dateadded" sortBy={sortBy} sortOrder={sortOrder} onSort={onSort} />
                </TableRow>
              </TableHeader>
              <TableBody>
                {leads.map((lead) => (
                  <TableRow key={lead.id}>
                    <TableCell>
                      <input
                        type="checkbox"
                        aria-label="Select lead"
                        checked={selectedLeadIds.has(lead.id)}
                        onChange={() => toggleLead(lead.id)}
                      />
                    </TableCell>
                    <TableCell className="font-medium">
                      {lead.firstName} {lead.lastName}
                    </TableCell>
                    <TableCell>{lead.email || '-'}</TableCell>
                    <TableCell>{lead.company ?? '-'}</TableCell>
                    <TableCell>{lead.jobTitle ?? '-'}</TableCell>
                    <TableCell>
                      <Select
                        className="h-8 w-36 text-xs"
                        value={String(lead.status)}
                        onChange={(e) =>
                          updateLeadStatus({ id: lead.id, status: Number(e.target.value) })
                        }
                      >
                        {Object.entries(leadStatusLabels).map(([value, label]) => (
                          <option key={value} value={value}>
                            {label}
                          </option>
                        ))}
                      </Select>
                    </TableCell>
                    <TableCell>
                      {lead.campaigns && lead.campaigns.length > 0
                        ? lead.campaigns.map((c) => c.name).join(', ')
                        : '-'}
                    </TableCell>
                    <TableCell>{formatDate(lead.createdAt)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            {totalPages > 1 && (
              <div className="mt-4 flex items-center justify-between gap-2">
                <p className="text-sm text-muted-foreground">
                  Page {leadPage} of {totalPages} · {totalLeads} lead(s)
                </p>
                <div className="flex items-center gap-2">
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={leadPage <= 1}
                    onClick={() => setLeadPage((p) => Math.max(1, p - 1))}
                  >
                    <ChevronLeft />
                    Previous
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={leadPage >= totalPages}
                    onClick={() => setLeadPage((p) => Math.min(totalPages, p + 1))}
                  >
                    Next
                    <ChevronRight />
                  </Button>
                </div>
              </div>
            )}
          </>
        )}
      </CardContent>
    </Card>
  )
}

function SortableHeader({
  label,
  sortKey,
  sortBy,
  sortOrder,
  onSort,
}: {
  label: string
  sortKey: SortKey
  sortBy: SortKey
  sortOrder: 'asc' | 'desc'
  onSort: (key: SortKey) => void
}) {
  const active = sortBy === sortKey
  return (
    <TableHead>
      <button
        type="button"
        className="inline-flex items-center gap-1 hover:text-foreground"
        onClick={() => onSort(sortKey)}
      >
        {label}
        {active ? (
          sortOrder === 'asc' ? (
            <ArrowUp className="h-3 w-3" />
          ) : (
            <ArrowDown className="h-3 w-3" />
          )
        ) : (
          <ChevronsUpDown className="h-3 w-3 opacity-40" />
        )}
      </button>
    </TableHead>
  )
}
