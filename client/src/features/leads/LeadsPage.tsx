import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Search, Download, Loader2 } from 'lucide-react'
import {
  useGetLeadsQuery,
  useImportFromRhetorikMutation,
  useSearchRhetorikMutation,
  useUpdateLeadStatusMutation,
} from '@/services/apiSlice'
import type { RhetorikSearchResponse } from '@/types/models'
import { LeadStatus, leadStatusLabels } from '@/types/models'
import type { RhetorikSearchRequest } from '@/types/models'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

interface SearchFormValues {
  keywords: string
  jobTitles: string
  companies: string
  countries: string
}

function toList(value: string): string[] | undefined {
  const items = value
    .split(',')
    .map((v) => v.trim())
    .filter(Boolean)
  return items.length > 0 ? items : undefined
}

function statusVariant(status: number) {
  switch (status) {
    case LeadStatus.New:
      return 'secondary'
    case LeadStatus.Contacted:
      return 'warning'
    case LeadStatus.Replied:
    case LeadStatus.Qualified:
      return 'success'
    case LeadStatus.OptedOut:
      return 'destructive'
    default:
      return 'outline'
  }
}

export function LeadsPage() {
  const { data: leads = [], isLoading: leadsLoading } = useGetLeadsQuery()
  const [searchRhetorik, { isLoading: searching }] = useSearchRhetorikMutation()
  const [importFromRhetorik, { isLoading: importing }] = useImportFromRhetorikMutation()
  const [updateLeadStatus] = useUpdateLeadStatusMutation()

  const [results, setResults] = useState<RhetorikSearchResponse | null>(null)
  const [lastRequest, setLastRequest] = useState<RhetorikSearchRequest | null>(null)

  const { register, handleSubmit } = useForm<SearchFormValues>()

  const onSearch = handleSubmit(async (values) => {
    const request: RhetorikSearchRequest = {
      keywords: toList(values.keywords),
      jobTitles: toList(values.jobTitles),
      companies: toList(values.companies),
      countries: toList(values.countries),
      pageNumber: 1,
      pageSize: 25,
    }
    const response = await searchRhetorik(request).unwrap()
    setResults(response)
    setLastRequest(request)
  })

  const onImport = async () => {
    if (!lastRequest) return
    await importFromRhetorik(lastRequest).unwrap()
    setResults(null)
    setLastRequest(null)
  }

  const totalResults = results?.counts?.contactsTotalResults ?? 0

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Find leads</CardTitle>
          <CardDescription>
            Search the Rhetorik360 contact database. Comma-separate multiple values. Searches are free;
            importing reveals contact data.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={onSearch} className="grid gap-4 md:grid-cols-4">
            <div className="space-y-1.5">
              <Label htmlFor="keywords">Keywords</Label>
              <Input id="keywords" placeholder="recruiter, talent" {...register('keywords')} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="jobTitles">Job titles</Label>
              <Input id="jobTitles" placeholder="Head of Talent" {...register('jobTitles')} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="companies">Companies</Label>
              <Input id="companies" placeholder="Acme Corp" {...register('companies')} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="countries">Countries</Label>
              <Input id="countries" placeholder="New Zealand" {...register('countries')} />
            </div>
            <div className="md:col-span-4">
              <Button type="submit" disabled={searching}>
                {searching ? <Loader2 className="animate-spin" /> : <Search />}
                Search
              </Button>
            </div>
          </form>

          {results && (
            <div className="mt-6 space-y-3">
              <div className="flex items-center justify-between">
                <p className="text-sm text-muted-foreground">
                  Approx. {totalResults.toLocaleString()} matches, showing{' '}
                  {results.results.length}
                </p>
                <Button onClick={onImport} disabled={importing || results.results.length === 0}>
                  {importing ? <Loader2 className="animate-spin" /> : <Download />}
                  Import all shown
                </Button>
              </div>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Company</TableHead>
                    <TableHead>Job title</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {results.results.map((r) => (
                    <TableRow key={r.contactData?.contactId ?? r.position}>
                      <TableCell className="font-medium">
                        {r.contactData
                          ? `${r.contactData.firstName} ${r.contactData.lastName}`
                          : '—'}
                      </TableCell>
                      <TableCell>{r.contactData?.companyName ?? '—'}</TableCell>
                      <TableCell>{r.contactData?.jobTitle ?? '—'}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Your leads ({leads.length})</CardTitle>
          <CardDescription>Imported contacts and their outreach status.</CardDescription>
        </CardHeader>
        <CardContent>
          {leadsLoading ? (
            <p className="text-sm text-muted-foreground">Loading…</p>
          ) : leads.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No leads yet - search above and import your first batch.
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Company</TableHead>
                  <TableHead>Job title</TableHead>
                  <TableHead>Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {leads.map((lead) => (
                  <TableRow key={lead.id}>
                    <TableCell className="font-medium">
                      {lead.firstName} {lead.lastName}
                    </TableCell>
                    <TableCell>{lead.email || '—'}</TableCell>
                    <TableCell>{lead.company ?? '—'}</TableCell>
                    <TableCell>{lead.jobTitle ?? '—'}</TableCell>
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
                      <Badge variant={statusVariant(lead.status)} className="sr-only">
                        {leadStatusLabels[lead.status]}
                      </Badge>
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
