import { useEffect, useState } from 'react'
import { ArrowLeft, Loader2, Save, Search } from 'lucide-react'
import {
  useCreateJobMutation,
  useGenerateJobSearchMutation,
  useGetClientsQuery,
  useGetJobQuery,
  useUpdateJobMutation,
} from '@/services/apiSlice'
import {
  Flexibility,
  flexibilityLabels,
  JobStatus,
  jobStatusLabels,
  JobType,
  jobTypeLabels,
  PayPeriod,
  payPeriodLabels,
  SalaryType,
  salaryTypeLabels,
  type JobInput,
  type ProfileSearchRequest,
} from '@/types/models'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { formatDate } from '@/lib/formatDate'
import { LookupField } from '@/components/LookupField'
import { LookupMultiField } from '@/components/LookupMultiField'

function emptyDraft(): JobInput {
  return {
    title: '',
    location: '',
    industry: '',
    type: JobType.FullTime,
    flexibility: Flexibility.OnSite,
    salaryType: SalaryType.NotDisclosed,
    salaryFrom: null,
    salaryTo: null,
    salaryNotes: '',
    startDate: '',
    expiryDate: '',
    hiringManager: '',
    department: '',
    advertUrl: '',
    advertCopy: '',
    mustHaves: '',
    niceToHaves: '',
    education: '',
    skills: '',
    attractiveReasons: '',
    screeningDetails: '',
    clientId: null,
    roleFamily: '',
    alternativeTitles: '',
    adjacentIndustries: '',
    vacancies: 1,
    shiftPattern: '',
    workingHours: '',
    onCallRequired: false,
    workAuthorisationRequired: true,
    sponsorshipAvailable: false,
    backgroundChecks: '',
    requiredLicences: '',
    exclusionCriteria: '',
    travelRequirement: '',
    relocationRequired: false,
    roleSummary: '',
    payCurrency: '',
    payPeriod: PayPeriod.Year,
    allowClientNameInMessages: false,
    status: JobStatus.Draft,
  }
}

type Step = 'basic' | 'brief' | 'advert' | 'review'

export function JobEditorPage({
  jobId,
  onBack,
  onSaved,
  onRunSearch,
}: {
  jobId?: number
  onBack: () => void
  onSaved: (id: number) => void
  onRunSearch: (id: number, criteria: ProfileSearchRequest) => void
}) {
  const isNew = jobId === undefined
  const { data: existing, isLoading } = useGetJobQuery(jobId as number, { skip: isNew })
  const [createJob, { isLoading: creating }] = useCreateJobMutation()
  const [updateJob, { isLoading: updating }] = useUpdateJobMutation()
  const [generateSearch, { isLoading: generating }] = useGenerateJobSearchMutation()
  const { data: clients = [] } = useGetClientsQuery()

  const [draft, setDraft] = useState<JobInput>(emptyDraft)
  const [initialised, setInitialised] = useState(false)
  const [step, setStep] = useState<Step>('basic')
  const [error, setError] = useState<string | null>(null)
  const [savedMessage, setSavedMessage] = useState<string | null>(null)

  useEffect(() => {
    if (isNew) {
      setInitialised(true)
      return
    }

    if (existing && !initialised) {
      setDraft({
        title: existing.title,
        location: existing.location ?? '',
        industry: existing.industry ?? '',
        type: existing.type,
        flexibility: existing.flexibility,
        salaryType: existing.salaryType,
        salaryFrom: existing.salaryFrom,
        salaryTo: existing.salaryTo,
        salaryNotes: existing.salaryNotes ?? '',
        startDate: existing.startDate ? existing.startDate.slice(0, 10) : '',
        expiryDate: existing.expiryDate ? existing.expiryDate.slice(0, 10) : '',
        hiringManager: existing.hiringManager ?? '',
        department: existing.department ?? '',
        advertUrl: existing.advertUrl ?? '',
        advertCopy: existing.advertCopy ?? '',
        mustHaves: existing.mustHaves ?? '',
        niceToHaves: existing.niceToHaves ?? '',
        education: existing.education ?? '',
        skills: existing.skills ?? '',
        attractiveReasons: existing.attractiveReasons ?? '',
        screeningDetails: existing.screeningDetails ?? '',
        clientId: existing.clientId ?? null,
        roleFamily: existing.roleFamily ?? '',
        alternativeTitles: existing.alternativeTitles ?? '',
        adjacentIndustries: existing.adjacentIndustries ?? '',
        vacancies: existing.vacancies ?? 1,
        shiftPattern: existing.shiftPattern ?? '',
        workingHours: existing.workingHours ?? '',
        onCallRequired: existing.onCallRequired ?? false,
        workAuthorisationRequired: existing.workAuthorisationRequired ?? true,
        sponsorshipAvailable: existing.sponsorshipAvailable ?? false,
        backgroundChecks: existing.backgroundChecks ?? '',
        requiredLicences: existing.requiredLicences ?? '',
        exclusionCriteria: existing.exclusionCriteria ?? '',
        travelRequirement: existing.travelRequirement ?? '',
        relocationRequired: existing.relocationRequired ?? false,
        roleSummary: existing.roleSummary ?? '',
        payCurrency: existing.payCurrency ?? '',
        payPeriod: existing.payPeriod ?? PayPeriod.Year,
        allowClientNameInMessages: existing.allowClientNameInMessages ?? false,
        status: existing.status,
      })
      setInitialised(true)
    }
  }, [existing, initialised, isNew])

  const updateDraft = (patch: Partial<JobInput>) => {
    setDraft((current) => ({ ...current, ...patch }))
    setSavedMessage(null)
  }

  const onSave = async (runSearch: boolean = false) => {
    setError(null)
    setSavedMessage(null)

    if (!draft.title.trim()) {
      setError('Job title is required.')
      return
    }

    const payload: JobInput = {
      ...draft,
      startDate: draft.startDate ? draft.startDate : null,
      expiryDate: draft.expiryDate ? draft.expiryDate : null,
      status: runSearch ? JobStatus.Active : draft.status,
    }

    try {
      let savedId: number
      if (isNew) {
        const created = await createJob(payload).unwrap()
        savedId = created.id
      } else {
        await updateJob({ id: jobId as number, ...payload }).unwrap()
        savedId = jobId as number
      }

      if (runSearch) {
        const criteria = await generateSearch(savedId).unwrap()
        onRunSearch(savedId, criteria)
      } else {
        onSaved(savedId)
      }
    } catch (e) {
      const data = (e as { data?: { error?: string; errors?: Record<string, string[]>; title?: string } })?.data
      const firstError = data?.errors
        ? Object.values(data.errors).flat()[0]
        : data?.error
      setError(firstError ?? data?.title ?? 'Could not save the job. Check the details and try again.')
    }
  }

  if (!isNew && isLoading) {
    return (
      <p className="flex items-center gap-2 text-sm text-muted-foreground">
        <Loader2 className="animate-spin" /> Loading job...
      </p>
    )
  }

  const saving = creating || updating

  const steps: { key: Step; label: string }[] = [
    { key: 'basic', label: 'Basic details' },
    { key: 'brief', label: 'Role brief' },
    { key: 'advert', label: 'Advert details' },
    { key: 'review', label: 'Review' },
  ]

  const stepIndex = steps.findIndex((s) => s.key === step)

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={onBack} aria-label="Back to jobs">
          <ArrowLeft />
        </Button>
        <div className="flex-1">
          <h2 className="text-xl font-semibold">{isNew ? 'New job' : draft.title || 'Edit job'}</h2>
        </div>
      </div>

      <div className="flex gap-2 border-b pb-2">
        {steps.map((s, i) => (
          <button
            key={s.key}
            type="button"
            onClick={() => setStep(s.key)}
            className={`rounded px-3 py-1.5 text-sm font-medium transition-colors ${
              i === stepIndex
                ? 'bg-primary text-primary-foreground'
                : 'text-muted-foreground hover:bg-accent'
            }`}
          >
            {i + 1}. {s.label}
          </button>
        ))}
      </div>

      {error && (
        <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}
      {savedMessage && (
        <p className="rounded-md border border-emerald-300 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
          {savedMessage}
        </p>
      )}

      {step === 'basic' && (
        <Card>
          <CardHeader>
            <CardTitle>Basic details</CardTitle>
            <CardDescription>Core job information for AI agent grounding.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label>Job title *</Label>
                <Input
                  value={draft.title}
                  onChange={(e) => updateDraft({ title: e.target.value })}
                  placeholder="e.g. Senior Software Engineer"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Location</Label>
                <Input
                  value={draft.location ?? ''}
                  onChange={(e) => updateDraft({ location: e.target.value })}
                  placeholder="e.g. Auckland, New Zealand"
                />
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-3">
              <div className="space-y-1.5">
                <Label>Client</Label>
                <Select
                  value={draft.clientId ? String(draft.clientId) : ''}
                  onChange={(e) =>
                    updateDraft({ clientId: e.target.value ? Number(e.target.value) : null })
                  }
                >
                  <option value="">No client assigned</option>
                  {clients.map((client) => (
                    <option key={client.id} value={client.id}>
                      {client.name}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>Role family</Label>
                <LookupField
                  category="role-family"
                  value={draft.roleFamily ?? ''}
                  onChange={(value) => updateDraft({ roleFamily: value })}
                  placeholder="e.g. Critical Facilities Engineer"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Vacancies</Label>
                <Input
                  type="number"
                  min={1}
                  value={draft.vacancies ?? 1}
                  onChange={(e) =>
                    updateDraft({ vacancies: Math.max(1, Number(e.target.value) || 1) })
                  }
                />
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-3">
              <div className="space-y-1.5">
                <Label>Industry</Label>
                <LookupField
                  category="industry"
                  value={draft.industry ?? ''}
                  onChange={(value) => updateDraft({ industry: value })}
                  placeholder="Select or type an industry"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Job type</Label>
                <Select
                  value={String(draft.type)}
                  onChange={(e) => updateDraft({ type: Number(e.target.value) })}
                >
                  {Object.entries(jobTypeLabels).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>Flexibility</Label>
                <Select
                  value={String(draft.flexibility)}
                  onChange={(e) => updateDraft({ flexibility: Number(e.target.value) })}
                >
                  {Object.entries(flexibilityLabels).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </Select>
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-4">
              <div className="space-y-1.5">
                <Label>Salary type</Label>
                <Select
                  value={String(draft.salaryType)}
                  onChange={(e) => updateDraft({ salaryType: Number(e.target.value) })}
                >
                  {Object.entries(salaryTypeLabels).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>Salary from</Label>
                <Input
                  type="number"
                  value={draft.salaryFrom ?? ''}
                  onChange={(e) =>
                    updateDraft({ salaryFrom: e.target.value ? Number(e.target.value) : null })
                  }
                  placeholder="0"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Salary to</Label>
                <Input
                  type="number"
                  value={draft.salaryTo ?? ''}
                  onChange={(e) =>
                    updateDraft({ salaryTo: e.target.value ? Number(e.target.value) : null })
                  }
                  placeholder="0"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Salary notes</Label>
                <Input
                  value={draft.salaryNotes ?? ''}
                  onChange={(e) => updateDraft({ salaryNotes: e.target.value })}
                  placeholder="e.g. Plus superannuation"
                />
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-3">
              <div className="space-y-1.5">
                <Label>Start date</Label>
                <Input
                  type="date"
                  value={draft.startDate ?? ''}
                  onChange={(e) => updateDraft({ startDate: e.target.value })}
                />
              </div>
              <div className="space-y-1.5">
                <Label>Expiry date</Label>
                <Input
                  type="date"
                  value={draft.expiryDate ?? ''}
                  onChange={(e) => updateDraft({ expiryDate: e.target.value })}
                />
              </div>
              <div className="space-y-1.5">
                <Label>Status</Label>
                <Select
                  value={String(draft.status)}
                  onChange={(e) => updateDraft({ status: Number(e.target.value) })}
                >
                  {Object.entries(jobStatusLabels).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </Select>
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label>Hiring manager</Label>
                <Input
                  value={draft.hiringManager ?? ''}
                  onChange={(e) => updateDraft({ hiringManager: e.target.value })}
                  placeholder="e.g. Jane Smith"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Department</Label>
                <Input
                  value={draft.department ?? ''}
                  onChange={(e) => updateDraft({ department: e.target.value })}
                  placeholder="e.g. Engineering"
                />
              </div>
            </div>

            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => setStep('brief')}
              >
                Next: Role brief
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {step === 'brief' && (
        <Card>
          <CardHeader>
            <CardTitle>Role brief</CardTitle>
            <CardDescription>
              The client brief the agent uses to qualify candidates. These become the interview-ready
              checks, so ask the client for all of this before the campaign starts.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label>Alternative job titles</Label>
                <LookupMultiField
                  category="job-title"
                  value={draft.alternativeTitles ?? ''}
                  onChange={(value) => updateDraft({ alternativeTitles: value })}
                  placeholder="Select or type a job title, then Add"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Adjacent industries / talent sources</Label>
                <LookupMultiField
                  category="industry"
                  value={draft.adjacentIndustries ?? ''}
                  onChange={(value) => updateDraft({ adjacentIndustries: value })}
                  placeholder="Select or type an industry, then Add"
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label>Approved role summary (candidate-facing)</Label>
              <Textarea
                rows={4}
                value={draft.roleSummary ?? ''}
                onChange={(e) => updateDraft({ roleSummary: e.target.value })}
                placeholder="A short, client-approved summary the agent shares with candidates."
              />
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label>Shift pattern</Label>
                <Input
                  value={draft.shiftPattern ?? ''}
                  onChange={(e) => updateDraft({ shiftPattern: e.target.value })}
                  placeholder="e.g. 12-hour rotating days/nights"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Working hours</Label>
                <Input
                  value={draft.workingHours ?? ''}
                  onChange={(e) => updateDraft({ workingHours: e.target.value })}
                  placeholder="e.g. 40 hours per week"
                />
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label>On-call required</Label>
                <Select
                  value={draft.onCallRequired ? 'yes' : 'no'}
                  onChange={(e) => updateDraft({ onCallRequired: e.target.value === 'yes' })}
                >
                  <option value="no">No</option>
                  <option value="yes">Yes</option>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>Travel requirement</Label>
                <Input
                  value={draft.travelRequirement ?? ''}
                  onChange={(e) => updateDraft({ travelRequirement: e.target.value })}
                  placeholder="e.g. Up to 25% travel"
                />
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-3">
              <div className="space-y-1.5">
                <Label>Right to work required</Label>
                <Select
                  value={draft.workAuthorisationRequired ? 'yes' : 'no'}
                  onChange={(e) => updateDraft({ workAuthorisationRequired: e.target.value === 'yes' })}
                >
                  <option value="yes">Yes - must have the right to work</option>
                  <option value="no">No</option>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>Sponsorship available</Label>
                <Select
                  value={draft.sponsorshipAvailable ? 'yes' : 'no'}
                  onChange={(e) => updateDraft({ sponsorshipAvailable: e.target.value === 'yes' })}
                >
                  <option value="no">No</option>
                  <option value="yes">Yes</option>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>Relocation required</Label>
                <Select
                  value={draft.relocationRequired ? 'yes' : 'no'}
                  onChange={(e) => updateDraft({ relocationRequired: e.target.value === 'yes' })}
                >
                  <option value="no">No</option>
                  <option value="yes">Yes</option>
                </Select>
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label>Background checks</Label>
                <Textarea
                  rows={2}
                  value={draft.backgroundChecks ?? ''}
                  onChange={(e) => updateDraft({ backgroundChecks: e.target.value })}
                  placeholder="e.g. Criminal record check, drug screening, security clearance"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Required licences & certifications</Label>
                <Textarea
                  rows={2}
                  value={draft.requiredLicences ?? ''}
                  onChange={(e) => updateDraft({ requiredLicences: e.target.value })}
                  placeholder="e.g. State electrical licence, HV switching ticket"
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label>Exclusion criteria</Label>
              <Textarea
                rows={2}
                value={draft.exclusionCriteria ?? ''}
                onChange={(e) => updateDraft({ exclusionCriteria: e.target.value })}
                placeholder="Anything that rules a candidate out."
              />
            </div>

            <div className="grid gap-4 sm:grid-cols-3">
              <div className="space-y-1.5">
                <Label>Pay currency</Label>
                <Input
                  value={draft.payCurrency ?? ''}
                  onChange={(e) => updateDraft({ payCurrency: e.target.value })}
                  placeholder="e.g. USD"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Pay period</Label>
                <Select
                  value={String(draft.payPeriod ?? PayPeriod.Year)}
                  onChange={(e) => updateDraft({ payPeriod: Number(e.target.value) })}
                >
                  {Object.entries(payPeriodLabels).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>Can messages name the client?</Label>
                <Select
                  value={draft.allowClientNameInMessages ? 'yes' : 'no'}
                  onChange={(e) => updateDraft({ allowClientNameInMessages: e.target.value === 'yes' })}
                >
                  <option value="no">No - describe the employer</option>
                  <option value="yes">Yes - may name the client</option>
                </Select>
              </div>
            </div>

            <div className="flex justify-between gap-2">
              <Button type="button" variant="outline" onClick={() => setStep('basic')}>
                Back: Basic details
              </Button>
              <Button type="button" onClick={() => setStep('advert')}>
                Next: Advert details
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {step === 'advert' && (
        <Card>
          <CardHeader>
            <CardTitle>Advert details</CardTitle>
            <CardDescription>Job description, requirements, and candidate-facing information.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-1.5">
              <Label>Advert URL</Label>
              <Input
                value={draft.advertUrl ?? ''}
                onChange={(e) => updateDraft({ advertUrl: e.target.value })}
                placeholder="https://..."
              />
              <p className="text-xs text-muted-foreground">
                Public URL where candidates can view and apply for this job. Used in outreach campaigns.
              </p>
            </div>

            <div className="space-y-1.5">
              <Label>Advert copy</Label>
              <Textarea
                rows={6}
                value={draft.advertCopy ?? ''}
                onChange={(e) => updateDraft({ advertCopy: e.target.value })}
                placeholder="Paste the full job advertisement here..."
              />
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label>Must-haves</Label>
                <Textarea
                  rows={3}
                  value={draft.mustHaves ?? ''}
                  onChange={(e) => updateDraft({ mustHaves: e.target.value })}
                  placeholder="Comma-separated: e.g. 5+ years experience, React, TypeScript"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Nice-to-haves</Label>
                <Textarea
                  rows={3}
                  value={draft.niceToHaves ?? ''}
                  onChange={(e) => updateDraft({ niceToHaves: e.target.value })}
                  placeholder="Comma-separated: e.g. AWS, Docker, leadership experience"
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label>Skills required</Label>
              <Textarea
                rows={3}
                value={draft.skills ?? ''}
                onChange={(e) => updateDraft({ skills: e.target.value })}
                placeholder="Comma-separated: e.g. JavaScript, Python, SQL"
              />
            </div>

            <div className="space-y-1.5">
              <Label>Education required</Label>
              <Textarea
                rows={2}
                value={draft.education ?? ''}
                onChange={(e) => updateDraft({ education: e.target.value })}
                placeholder="e.g. Bachelor's degree in Computer Science or equivalent"
              />
            </div>

            <div className="space-y-1.5">
              <Label>Reasons the role is attractive</Label>
              <Textarea
                rows={3}
                value={draft.attractiveReasons ?? ''}
                onChange={(e) => updateDraft({ attractiveReasons: e.target.value })}
                placeholder="e.g. Fast-growing startup, remote-first culture, competitive salary"
              />
            </div>

            <div className="space-y-1.5">
              <Label>Screening / application process details</Label>
              <Textarea
                rows={3}
                value={draft.screeningDetails ?? ''}
                onChange={(e) => updateDraft({ screeningDetails: e.target.value })}
                placeholder="e.g. Initial phone screen, technical interview, final round with hiring manager"
              />
            </div>

            <div className="flex justify-between gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => setStep('brief')}
              >
                Back: Role brief
              </Button>
              <Button
                type="button"
                onClick={() => setStep('review')}
              >
                Next: Review
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {step === 'review' && (
        <Card>
          <CardHeader>
            <CardTitle>Review job details</CardTitle>
            <CardDescription>Check the information before saving.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="rounded-md border bg-muted/30 p-4">
              <h3 className="mb-2 font-semibold">Basic details</h3>
              <dl className="grid gap-2 sm:grid-cols-2 text-sm">
                <div><dt className="font-medium">Title:</dt><dd>{draft.title || '—'}</dd></div>
                <div><dt className="font-medium">Location:</dt><dd>{draft.location || '—'}</dd></div>
                <div><dt className="font-medium">Industry:</dt><dd>{draft.industry || '—'}</dd></div>
                <div><dt className="font-medium">Client:</dt><dd>{clients.find((c) => c.id === draft.clientId)?.name ?? '—'}</dd></div>
                <div><dt className="font-medium">Role family:</dt><dd>{draft.roleFamily || '—'}</dd></div>
                <div><dt className="font-medium">Vacancies:</dt><dd>{draft.vacancies ?? 1}</dd></div>
                <div><dt className="font-medium">Type:</dt><dd>{jobTypeLabels[draft.type ?? 0]}</dd></div>
                <div><dt className="font-medium">Flexibility:</dt><dd>{flexibilityLabels[draft.flexibility ?? 0]}</dd></div>
                <div><dt className="font-medium">Salary:</dt><dd>{salaryTypeLabels[draft.salaryType ?? 3]}{draft.salaryFrom ? ` $${draft.salaryFrom}` : ''}{draft.salaryTo ? ` - $${draft.salaryTo}` : ''}{draft.salaryNotes ? ` (${draft.salaryNotes})` : ''}</dd></div>
                <div><dt className="font-medium">Start date:</dt><dd>{formatDate(draft.startDate)}</dd></div>
                <div><dt className="font-medium">Expiry date:</dt><dd>{formatDate(draft.expiryDate)}</dd></div>
                <div><dt className="font-medium">Hiring manager:</dt><dd>{draft.hiringManager || '—'}</dd></div>
                <div><dt className="font-medium">Department:</dt><dd>{draft.department || '—'}</dd></div>
                <div><dt className="font-medium">Status:</dt><dd>{jobStatusLabels[draft.status ?? 0]}</dd></div>
              </dl>
            </div>

            <div className="rounded-md border bg-muted/30 p-4">
              <h3 className="mb-2 font-semibold">Role brief</h3>
              <dl className="grid gap-2 text-sm">
                <div><dt className="font-medium">Shift pattern:</dt><dd>{draft.shiftPattern || '—'}</dd></div>
                <div><dt className="font-medium">Working hours:</dt><dd>{draft.workingHours || '—'}</dd></div>
                <div><dt className="font-medium">On-call required:</dt><dd>{draft.onCallRequired ? 'Yes' : 'No'}</dd></div>
                <div><dt className="font-medium">Travel:</dt><dd>{draft.travelRequirement || '—'}</dd></div>
                <div><dt className="font-medium">Right to work required:</dt><dd>{draft.workAuthorisationRequired ? 'Yes' : 'No'}</dd></div>
                <div><dt className="font-medium">Sponsorship available:</dt><dd>{draft.sponsorshipAvailable ? 'Yes' : 'No'}</dd></div>
                <div><dt className="font-medium">Relocation required:</dt><dd>{draft.relocationRequired ? 'Yes' : 'No'}</dd></div>
                <div><dt className="font-medium">Pay:</dt><dd>{(draft.payCurrency || '') + ' ' + (payPeriodLabels[draft.payPeriod ?? PayPeriod.Year] ?? '')}</dd></div>
                <div><dt className="font-medium">Can name client:</dt><dd>{draft.allowClientNameInMessages ? 'Yes' : 'No'}</dd></div>
                <div><dt className="font-medium">Alternative titles:</dt><dd>{draft.alternativeTitles || '—'}</dd></div>
                <div><dt className="font-medium">Adjacent industries:</dt><dd>{draft.adjacentIndustries || '—'}</dd></div>
                <div><dt className="font-medium">Background checks:</dt><dd className="whitespace-pre-wrap">{draft.backgroundChecks || '—'}</dd></div>
                <div><dt className="font-medium">Required licences:</dt><dd className="whitespace-pre-wrap">{draft.requiredLicences || '—'}</dd></div>
                <div><dt className="font-medium">Exclusion criteria:</dt><dd className="whitespace-pre-wrap">{draft.exclusionCriteria || '—'}</dd></div>
                <div><dt className="font-medium">Role summary:</dt><dd className="whitespace-pre-wrap">{draft.roleSummary || '—'}</dd></div>
              </dl>
            </div>

            <div className="rounded-md border bg-muted/30 p-4">
              <h3 className="mb-2 font-semibold">Advert details</h3>
              <dl className="grid gap-2 text-sm">
                <div><dt className="font-medium">Advert URL:</dt><dd className="break-all">{draft.advertUrl || '—'}</dd></div>
                <div><dt className="font-medium">Advert copy:</dt><dd className="whitespace-pre-wrap">{draft.advertCopy || '—'}</dd></div>
                <div><dt className="font-medium">Must-haves:</dt><dd className="whitespace-pre-wrap">{draft.mustHaves || '—'}</dd></div>
                <div><dt className="font-medium">Nice-to-haves:</dt><dd className="whitespace-pre-wrap">{draft.niceToHaves || '—'}</dd></div>
                <div><dt className="font-medium">Skills:</dt><dd className="whitespace-pre-wrap">{draft.skills || '—'}</dd></div>
                <div><dt className="font-medium">Education:</dt><dd className="whitespace-pre-wrap">{draft.education || '—'}</dd></div>
                <div><dt className="font-medium">Attractive reasons:</dt><dd className="whitespace-pre-wrap">{draft.attractiveReasons || '—'}</dd></div>
                <div><dt className="font-medium">Screening details:</dt><dd className="whitespace-pre-wrap">{draft.screeningDetails || '—'}</dd></div>
              </dl>
            </div>

            <div className="flex justify-between gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => setStep('advert')}
              >
                Back: Advert details
              </Button>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => onSave(false)}
                  disabled={saving}
                >
                  {saving ? <Loader2 className="animate-spin" /> : <Save />}
                  Save
                </Button>
                <Button
                  type="button"
                  onClick={() => onSave(true)}
                  disabled={saving || generating}
                >
                  {saving || generating ? <Loader2 className="animate-spin" /> : <Search />}
                  Save & run search
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
