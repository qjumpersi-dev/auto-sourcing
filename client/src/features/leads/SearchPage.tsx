import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { AlertCircle, Download, Loader2, MailPlus, Search, Sparkles } from 'lucide-react'
import {
  useGetCampaignsQuery,
  useGenerateSearchSpecMutation,
  useImportFromRhetorikMutation,
  useImportToCampaignMutation,
  useSearchRhetorikMutation,
} from '@/services/apiSlice'
import type { ProfileSearchRequest, EnrichedProfileSearchResponse, Scope, ExpertiseMode } from '@/types/models'
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
  const items = (value ?? '').split(',').map((v) => v.trim()).filter(Boolean)
  return items.length > 0 ? items : undefined
}

function companyName(r: EnrichedProfileSearchResponse['results'][number]): string | null {
  const exp =
    r.contact_data?.contact_current_experiences?.find((e) => e.current) ??
    r.contact_data?.contact_current_experiences?.[0]
  return exp?.raw_company_name ?? exp?.company_name ?? null
}

function jobTitle(r: EnrichedProfileSearchResponse['results'][number]): string | null {
  const exp =
    r.contact_data?.contact_current_experiences?.find((e) => e.current) ??
    r.contact_data?.contact_current_experiences?.[0]
  return exp?.job_title ?? r.profile_data?.profile_headline ?? null
}

export function SearchPage() {
  const [searchRhetorik, { isLoading: searching }] = useSearchRhetorikMutation()
  const [generateSpec] = useGenerateSearchSpecMutation()
  const [importFromRhetorik, { isLoading: importing }] = useImportFromRhetorikMutation()
  const [importToCampaign, { isLoading: addingToCampaign }] = useImportToCampaignMutation()
  const { data: campaigns = [] } = useGetCampaignsQuery()

  const [results, setResults] = useState<EnrichedProfileSearchResponse | null>(null)
  const [lastRequest, setLastRequest] = useState<ProfileSearchRequest | null>(null)
  const [searchError, setSearchError] = useState<string | null>(null)
  const [selectedResultIds, setSelectedResultIds] = useState<Set<string>>(new Set())
  const [targetCampaign, setTargetCampaign] = useState('')
  const [addFeedback, setAddFeedback] = useState<string | null>(null)
  const [jobTitleSuggestions, setJobTitleSuggestions] = useState<string[]>([])

  const { register, handleSubmit, getValues, setValue, formState: { errors } } = useForm<SearchFormValues>({
    defaultValues: {
      freeText: '',
      keywords: '',
      jobTitles: '',
      jobTitleScope: 'any',
      companies: '',
      companyScope: 'current',
      expertises: '',
      expertiseMode: 'must_have_any',
      country: '',
      states: '',
      cities: '',
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
      countries: (v.country ?? '').trim() ? [(v.country ?? '').trim()] : undefined,
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
      if (spec.jobTitles?.length) setValue('jobTitles', spec.jobTitles.join(', '))
      if (spec.jobTitleScope) setValue('jobTitleScope', spec.jobTitleScope)
      if (spec.companies?.length) setValue('companies', spec.companies.join(', '))
      if (spec.companyScope) setValue('companyScope', spec.companyScope)
      if (spec.expertises?.length) setValue('expertises', spec.expertises.join(', '))
      if (spec.expertiseMode) setValue('expertiseMode', spec.expertiseMode)
      if (spec.countries?.length) setValue('country', spec.countries[0])
      if (spec.states?.length) setValue('states', spec.states.join(', '))
      if (spec.cities?.length) setValue('cities', spec.cities.join(', '))
      setJobTitleSuggestions(spec.jobTitleSuggestions ?? [])
    } catch {
      setSearchError('Could not auto-build the search. Fill the fields manually.')
    }
  }

  const refreshResults = async () => {
    if (!lastRequest) return
    try {
      const response = await searchRhetorik(lastRequest).unwrap()
      setResults(response)
    } catch {
      setSearchError('Could not refresh search results.')
    }
  }

  const onSearch = handleSubmit(async () => {
    setSearchError(null)
    setAddFeedback(null)
    setResults(null)
    setSelectedResultIds(new Set())
    try {
      const request = buildRequest()
      const response = await searchRhetorik(request).unwrap()
      setLastRequest(request)
      setResults(response)
    } catch {
      setSearchError('The search failed. Rhetorik may be having issues - try again or simplify the filters.')
    }
  })

  const toggleResult = (profileId: string) => {
    setSelectedResultIds((prev) => {
      const next = new Set(prev)
      if (next.has(profileId)) {
        next.delete(profileId)
      } else {
        next.add(profileId)
      }
      return next
    })
  }

  const allResultsSelected =
    (results?.results.length ?? 0) > 0 &&
    (results?.results ?? []).every((r) => r.profile_data && selectedResultIds.has(r.profile_data.profile_id))

  const toggleAllResults = () => {
    if (!results) return
    if (allResultsSelected) {
      setSelectedResultIds(new Set())
    } else {
      setSelectedResultIds(
        new Set(results.results.map((r) => r.profile_data?.profile_id).filter((x): x is string => !!x)),
      )
    }
  }

  const onImportSelected = async () => {
    if (selectedResultIds.size === 0 || !lastRequest) return
    setSearchError(null)
    try {
      await importFromRhetorik({ ...lastRequest, profileIds: [...selectedResultIds] }).unwrap()
      setSelectedResultIds(new Set())
      await refreshResults()
    } catch {
      setSearchError('Import failed. Please try again.')
    }
  }

  const onImportAll = async () => {
    if (!lastRequest) return
    setSearchError(null)
    try {
      await importFromRhetorik(lastRequest).unwrap()
      setSelectedResultIds(new Set())
      await refreshResults()
    } catch {
      setSearchError('Import failed. Please try again.')
    }
  }

  const onAddToCampaign = async () => {
    if (!targetCampaign || selectedResultIds.size === 0) return
    setSearchError(null)
    setAddFeedback(null)
    try {
      const result = await importToCampaign({
        campaignId: Number(targetCampaign),
        profileIds: [...selectedResultIds],
      }).unwrap()
      setAddFeedback(
        `Added ${result.added} candidate(s) to the campaign.${result.skipped > 0 ? ` ${result.skipped} skipped (already in campaign or opted out).` : ''}`,
      )
      setSelectedResultIds(new Set())
      await refreshResults()
    } catch {
      setAddFeedback('Could not add candidates to the campaign. Does the campaign have templates set?')
    }
  }

  const appendJobTitleSuggestion = (s: string) => {
    const current = getValues('jobTitles') ?? ''
    const parts = current.split(',').map((p) => p.trim()).filter(Boolean)
    if (!parts.some((p) => p.toLowerCase() === s.toLowerCase())) {
      setValue('jobTitles', [...parts, s].join(', '))
    }
    setJobTitleSuggestions((prev) => prev.filter((x) => x !== s))
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Find candidates</CardTitle>
        <CardDescription>
          Searches Rhetorik360 profiles. Every search automatically includes the "Profile Has Email"
          tag. Up to 500 profiles are shown per search.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-5">
        <div className="space-y-1.5">
          <Label htmlFor="freeText">Describe who you are looking for</Label>
          <div className="flex gap-2">
            <Input
              id="freeText"
              placeholder='e.g. "Senior .NET developers in Auckland with Azure skills"'
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
            {jobTitleSuggestions.length > 0 && (
              <div className="flex flex-wrap gap-1.5 pt-1">
                <span className="text-xs text-muted-foreground">Also try:</span>
                {jobTitleSuggestions.map((s) => (
                  <button
                    key={s}
                    type="button"
                    className="rounded-full border px-2 py-0.5 text-xs hover:bg-accent"
                    onClick={() => appendJobTitleSuggestion(s)}
                  >
                    + {s}
                  </button>
                ))}
              </div>
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
            <div className="flex flex-wrap items-center justify-between gap-2">
              <p className="text-sm font-medium">
                Showing {results.results.length.toLocaleString()} of approx.{' '}
                {(results.counts?.profiles_total_results ?? 0).toLocaleString()} matching profiles (max
                500 shown)
              </p>
              <div className="flex flex-wrap items-center gap-2">
                <Select
                  className="h-8 w-48 text-xs"
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
                  disabled={addingToCampaign || selectedResultIds.size === 0 || !targetCampaign}
                  onClick={onAddToCampaign}
                >
                  {addingToCampaign ? <Loader2 className="animate-spin" /> : <MailPlus />}
                  Add to campaign ({selectedResultIds.size})
                </Button>
              </div>
            </div>

            {addFeedback && (
              <p className="text-xs text-muted-foreground">{addFeedback}</p>
            )}

            <div className="flex flex-wrap items-center gap-2">
              <Button
                size="sm"
                variant="outline"
                onClick={onImportSelected}
                disabled={importing || selectedResultIds.size === 0}
              >
                {importing ? <Loader2 className="animate-spin" /> : <Download />}
                Save selected ({selectedResultIds.size})
              </Button>
              <Button
                size="sm"
                variant="outline"
                onClick={onImportAll}
                disabled={importing || results.results.length === 0}
              >
                {importing ? <Loader2 className="animate-spin" /> : <Download />}
                Save all shown
              </Button>
            </div>

            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10">
                    <input
                      type="checkbox"
                      aria-label="Select all results"
                      checked={allResultsSelected}
                      onChange={toggleAllResults}
                    />
                  </TableHead>
                  <TableHead>Name</TableHead>
                  <TableHead>Company</TableHead>
                  <TableHead>Job title</TableHead>
                  <TableHead>Location</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Campaign</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {results.results.map((r) => {
                  const c = companyName(r)
                  return (
                    <TableRow key={r.profile_data?.profile_id ?? r.position}>
                      <TableCell>
                        <input
                          type="checkbox"
                          aria-label="Select result"
                          checked={!!r.profile_data && selectedResultIds.has(r.profile_data.profile_id)}
                          onChange={() => {
                            if (r.profile_data) toggleResult(r.profile_data.profile_id)
                          }}
                        />
                      </TableCell>
                      <TableCell className="font-medium">
                        {r.profile_data ? `${r.profile_data.profile_first_name} ${r.profile_data.profile_last_name}` : '-'}
                      </TableCell>
                      <TableCell>{c ?? '-'}</TableCell>
                      <TableCell>{jobTitle(r) ?? '-'}</TableCell>
                      <TableCell>
                        {[r.profile_data?.profile_address?.city, r.profile_data?.profile_address?.country]
                          .filter(Boolean)
                          .join(', ') || '-'}
                      </TableCell>
                      <TableCell>
                        {r.lead_id ? (
                          <Badge variant="secondary">Added</Badge>
                        ) : (
                          <Badge variant="outline">Not added</Badge>
                        )}
                      </TableCell>
                      <TableCell>
                        {r.campaigns && r.campaigns.length > 0
                          ? r.campaigns.map((camp) => camp.name).join(', ')
                          : '-'}
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
  )
}
