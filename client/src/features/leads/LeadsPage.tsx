import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { AlertCircle, Download, Loader2, Search, Sparkles } from 'lucide-react'
import {
  useGenerateSearchSpecMutation,
  useGetLeadsQuery,
  useImportFromRhetorikMutation,
  useSearchRhetorikMutation,
  useUpdateLeadStatusMutation,
} from '@/services/apiSlice'
import type { ProfileSearchRequest, ProfileSearchResponse, Scope, ExpertiseMode } from '@/types/models'
import { leadStatusLabels } from '@/types/models'
import { SuggestionInput } from '@/components/Combobox'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'

interface SearchFormValues {
  freeText: string
  keywords: string
  jobTitles: string
  jobTitleScope: Scope
  companies: string
  companyScope: Scope
  expertises: string
  expertiseMode: ExpertiseMode
  country: string
  states: string
  cities: string
}

const expertiseModeLabels: Record<ExpertiseMode, string> = {
  must_have_any: 'Must have any',
  must_have_all: 'Must have all',
  must_not_have_any: 'Must not have any',
  must_not_have_all: 'Must not have all',
}

function toList(value: string): string[] | undefined {
  const items = value.split(',').map((v) => v.trim()).filter(Boolean)
  return items.length > 0 ? items : undefined
}

export function LeadsPage() {
  const { data: leads = [], isLoading: leadsLoading } = useGetLeadsQuery()
  const [searchRhetorik, { isLoading: searching }] = useSearchRhetorikMutation()
  const [generateSpec] = useGenerateSearchSpecMutation()
  const [importFromRhetorik, { isLoading: importing }] = useImportFromRhetorikMutation()
  const [updateLeadStatus] = useUpdateLeadStatusMutation()

  const [results, setResults] = useState<ProfileSearchResponse | null>(null)
  const [lastRequest, setLastRequest] = useState<ProfileSearchRequest | null>(null)
  const [searchError, setSearchError] = useState<string | null>(null)

  const { register, handleSubmit, getValues, setValue, formState: { errors } } = useForm<SearchFormValues>({
    defaultValues: {
      jobTitleScope: 'any',
      companyScope: 'current',
      expertiseMode: 'must_have_any',
    },
  })

  const buildRequest = (): ProfileSearchRequest => {
    const v = getValues()
    return {
      keywords: toList(v.keywords),
      jobTitles: toList(v.jobTitles),
      jobTitleScope: v.jobTitleScope,
      companies: toList(v.companies),
      companyScope: v.companyScope,
      expertises: toList(v.expertises),
      expertiseMode: v.expertiseMode,
      countries: v.country.trim() ? [v.country.trim()] : undefined,
      states: toList(v.states),
      cities: toList(v.cities),
      pageNumber: 1,
      pageSize: 100,
      maxResults: 500,
    }
  }

  const onAutoBuild = async () => {
    const text = getValues().freeText.trim()
    if (!text) return
    setSearchError(null)
    try {
      const spec = await generateSpec({ text }).unwrap()
      if (spec.keywords?.length) setValue('keywords', spec.keywords.join(', '))
      if (spec.jobTitles?.length) setValue('jobTitles', spec.jobTitles.join(', '))
      if (spec.jobTitleScope) setValue('jobTitleScope', spec.jobTitleScope)
      if (spec.companies?.length) setValue('companies', spec.companies.join(', '))
      if (spec.companyScope) setValue('companyScope', spec.companyScope)
      if (spec.expertises?.length) setValue('expertises', spec.expertises.join(', '))
      if (spec.expertiseMode) setValue('expertiseMode', spec.expertiseMode)
      if (spec.countries?.length) setValue('country', spec.countries[0])
      if (spec.states?.length) setValue('states', spec.states.join(', '))
      if (spec.cities?.length) setValue('cities', spec.cities.join(', '))
    } catch {
      setSearchError('Could not auto-build the search. Fill the fields manually.')
    }
  }

  const onSearch = handleSubmit(async () => {
    setSearchError(null)
    setResults(null)
    try {
      const request = buildRequest()
      const response = await searchRhetorik(request).unwrap()
      setLastRequest(request)
      setResults(response)
    } catch {
      setSearchError(
        'The search failed. Rhetorik may be having issues - try again or simplify the filters.',
      )
    }
  })

  const onImport = async () => {
    if (!lastRequest) return
    setSearchError(null)
    try {
      await importFromRhetorik(lastRequest).unwrap()
      setResults(null)
    } catch {
      setSearchError('Import failed. Please try again.')
    }
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Find leads</CardTitle>
          <CardDescription>
            Searches Rhetorik360 profiles. Every search automatically includes the "Profile Has
            Email" tag. Up to 500 profiles are shown per search.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-5">
          <div className="space-y-1.5">
            <Label htmlFor="freeText">Describe who you are looking for</Label>
            <div className="flex gap-2">
              <Input
                id="freeText"
                placeholder='e.g. "Senior .NET developers in Auckland with AWS skills"'
                {...register('freeText')}
              />
              <Button type="button" variant="outline" onClick={onAutoBuild}>
                <Sparkles />
                Auto-build search
              </Button>
            </div>
          </div>

          <form onSubmit={onSearch} className="grid gap-4 md:grid-cols-3">
            <div className="space-y-1.5">
              <Label htmlFor="keywords">Keywords</Label>
              <Input id="keywords" placeholder="recruiter, talent" {...register('keywords')} />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="jobTitles">Job titles</Label>
              <Input id="jobTitles" placeholder="Developer" {...register('jobTitles')} />
              {errors.jobTitles && (
                <p className="text-xs text-destructive">{errors.jobTitles.message}</p>
              )}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="jobTitleScope">Job title scope</Label>
              <Select id="jobTitleScope" {...register('jobTitleScope')}>
                <option value="any">Any</option>
                <option value="current">Current</option>
                <option value="past">Past</option>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="companies">Companies</Label>
              <Input id="companies" placeholder="Acme Corp" {...register('companies')} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="companyScope">Company scope</Label>
              <Select id="companyScope" {...register('companyScope')}>
                <option value="current">Current</option>
                <option value="any">Any</option>
                <option value="past">Past</option>
              </Select>
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="expertiseMode">Skills rule</Label>
              <Select id="expertiseMode" {...register('expertiseMode')}>
                {(Object.keys(expertiseModeLabels) as ExpertiseMode[]).map((m) => (
                  <option key={m} value={m}>
                    {expertiseModeLabels[m]}
                  </option>
                ))}
              </Select>
            </div>

            <div className="space-y-1.5 md:col-span-2">
              <Label htmlFor="expertises">Skills / expertise</Label>
              <Input id="expertises" placeholder="C#, SQL, Azure" {...register('expertises')} />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="country">Country</Label>
              <SuggestionInput
                field="countries"
                value={getValues().country}
                onChange={(v) => setValue('country', v)}
                placeholder="Start typing a country..."
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="states">Region / State</Label>
              <Input id="states" placeholder="Auckland" {...register('states')} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="cities">City</Label>
              <Input id="cities" placeholder="Wellington" {...register('cities')} />
            </div>

            <div className="flex items-center gap-3 md:col-span-3">
              <Button type="submit" disabled={searching}>
                {searching ? <Loader2 className="animate-spin" /> : <Search />}
                Search profiles
              </Button>
              <Badge variant="secondary">Must have email: always on</Badge>
            </div>
          </form>

          {searchError && (
            <div className="flex items-start gap-2 rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
              <span>{searchError}</span>
            </div>
          )}

          {results && (
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <p className="text-sm font-medium">
                  Showing {results.results.length.toLocaleString()} of approx.{' '}
                  {(results.counts?.profiles_total_results ?? 0).toLocaleString()} matching profiles
                  (max 500 shown)
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
                    <TableHead>Location</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {results.results.map((r) => {
                    const exp =
                      r.contact_data?.contact_current_experiences?.find((e) => e.current) ??
                      r.contact_data?.contact_current_experiences?.[0]
                    return (
                      <TableRow key={r.profile_data?.profile_id ?? r.position}>
                        <TableCell className="font-medium">
                          {r.profile_data
                            ? `${r.profile_data.profile_first_name} ${r.profile_data.profile_last_name}`
                            : '-'}
                        </TableCell>
                        <TableCell>{exp?.company_name ?? '-'}</TableCell>
                        <TableCell>
                          {exp?.job_title ?? r.profile_data?.profile_headline ?? '-'}
                        </TableCell>
                        <TableCell>
                          {[
                            r.profile_data?.profile_address?.city,
                            r.profile_data?.profile_address?.country,
                          ]
                            .filter(Boolean)
                            .join(', ') || '-'}
                        </TableCell>
                      </TableRow>
                    )
                  })}
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
            <p className="text-sm text-muted-foreground">Loading...</p>
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
