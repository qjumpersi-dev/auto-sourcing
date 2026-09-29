import { useEffect, useState } from 'react'
import { ArrowLeft, Eye, Loader2, Plus, Save, Sparkles, Trash2 } from 'lucide-react'
import {
  useCreateSequenceMutation,
  useGetJobsQuery,
  useGetLeadsQuery,
  useGetPersonalisationOptionsQuery,
  useGetSequenceQuery,
  usePreviewSequenceMutation,
  useUpdateSequenceMutation,
  useGenerateContentMutation,
} from '@/services/apiSlice'
import {
  outreachChannelLabels,
  OutreachChannel,
  SequenceStatus,
  sequenceStatusLabels,
  SequenceStepCondition,
  sequenceStepConditionLabels,
  weekDays,
  type SequencePreview,
  type SequenceInput,
  type SequenceStepInput,
} from '@/types/models'
import { RichTextEditor, type EditorAttributeGroup } from '@/components/RichTextEditor'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'
import { formatSendDays, formatTimeWindow } from './sequenceUtils'

function stripHtml(html: string): string {
  return html.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim()
}

function newStep(index: number): SequenceStepInput {
  return {
    id: 0,
    name: `Step ${index + 1}`,
    channel: OutreachChannel.Email,
    subjectTemplate: '',
    bodyTemplate: '',
    delayDays: index === 0 ? 0 : 3,
    condition: SequenceStepCondition.Always,
  }
}

function emptyDraft(): SequenceInput {
  return {
    name: '',
    description: '',
    status: SequenceStatus.Draft,
    sendDaysMask: 62,
    sendWindowStart: '09:00',
    sendWindowEnd: '17:00',
    includeUnsubscribe: true,
    steps: [newStep(0)],
  }
}

export function SequenceEditorPage({
  sequenceId,
  onBack,
  onSaved,
}: {
  sequenceId?: number
  onBack: () => void
  onSaved: (id: number) => void
}) {
  const isNew = sequenceId === undefined
  const { data: existing, isLoading } = useGetSequenceQuery(sequenceId as number, { skip: isNew })
  const { data: leadsData } = useGetLeadsQuery({ page: 1, pageSize: 100 })
  const { data: personalisations = [] } = useGetPersonalisationOptionsQuery()
  const [createSequence, { isLoading: creating }] = useCreateSequenceMutation()
  const [updateSequence, { isLoading: updating }] = useUpdateSequenceMutation()
  const [previewSequence, { isLoading: previewing }] = usePreviewSequenceMutation()
  const [generateContent] = useGenerateContentMutation()
  const { data: jobs = [] } = useGetJobsQuery()

  const [draft, setDraft] = useState<SequenceInput>(emptyDraft)
  const [initialised, setInitialised] = useState(false)
  const [selectedStep, setSelectedStep] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const [savedMessage, setSavedMessage] = useState<string | null>(null)
  const [previewLeadId, setPreviewLeadId] = useState('')
  const [preview, setPreview] = useState<SequencePreview | null>(null)
  const [generateJobId, setGenerateJobId] = useState('')
  const [generating, setGenerating] = useState(false)
  const [generateError, setGenerateError] = useState<string | null>(null)

  useEffect(() => {
    if (isNew) {
      setInitialised(true)
      return
    }

    if (existing && !initialised) {
      setDraft({
        name: existing.name,
        description: existing.description ?? '',
        status: existing.status,
        sendDaysMask: existing.sendDaysMask,
        sendWindowStart: existing.sendWindowStart.slice(0, 5),
        sendWindowEnd: existing.sendWindowEnd.slice(0, 5),
        includeUnsubscribe: existing.includeUnsubscribe,
        steps: existing.steps.map((step) => ({
          id: step.id,
          name: step.name,
          channel: step.channel,
          subjectTemplate: step.subjectTemplate ?? '',
          bodyTemplate: step.bodyTemplate,
          delayDays: step.delayDays,
          condition: step.condition,
        })),
      })
      setInitialised(true)
    }
  }, [existing, initialised, isNew])

  const leads = leadsData?.items ?? []
  const step = draft.steps[selectedStep]
  const isEmail = step?.channel === OutreachChannel.Email

  const attributeGroups: EditorAttributeGroup[] = [
    {
      label: 'Contact attributes',
      items: [
        { label: 'Company name', token: '{{Company}}' },
        { label: 'First name', token: '{{FirstName}}' },
        { label: 'Last name', token: '{{LastName}}' },
        { label: 'Current job title', token: '{{JobTitle}}' },
        { label: 'Current location', token: '{{Location}}' },
        { label: 'Job URL', token: '{{JobUrl}}' },
        { label: 'Organization name', token: '{{OrgName}}' },
        { label: 'Job location', token: '{{Location}}' },
        { label: 'Consent link', token: '{{ConsentUrl}}' },
        { label: 'Lead company (candidate)', token: '{{LeadCompany}}' },
        { label: 'Lead location (candidate)', token: '{{LeadLocation}}' },
      ],
    },
    {
      label: 'AI personalisation',
      items: personalisations.map((option) => ({
        label: option.name,
        token: `{{Personalisation:${option.key}}}`,
      })),
    },
  ]

  const updateDraft = (patch: Partial<SequenceInput>) => {
    setDraft((current) => ({ ...current, ...patch }))
    setSavedMessage(null)
  }

  const updateStep = (index: number, patch: Partial<SequenceStepInput>) => {
    setDraft((current) => ({
      ...current,
      steps: current.steps.map((item, i) => (i === index ? { ...item, ...patch } : item)),
    }))
    setSavedMessage(null)
  }

  const addStep = () => {
    setDraft((current) => ({ ...current, steps: [...current.steps, newStep(current.steps.length)] }))
    setSelectedStep(draft.steps.length)
    setPreview(null)
  }

  const removeStep = (index: number) => {
    if (draft.steps.length === 1) return
    setDraft((current) => ({
      ...current,
      steps: current.steps
        .filter((_, i) => i !== index)
        .map((item, i) => ({ ...item, name: item.name || `Step ${i + 1}` })),
    }))
    setSelectedStep((current) => Math.max(0, current >= index ? current - 1 : current))
    setPreview(null)
  }

  const toggleDay = (bit: number) => {
    updateDraft({ sendDaysMask: draft.sendDaysMask ^ bit })
  }

  const onSave = async () => {
    setError(null)
    setSavedMessage(null)

    if (!draft.name.trim()) {
      setError('Give the sequence a name.')
      return
    }
    if (draft.steps.some((s) => !s.subjectTemplate.trim())) {
      setError('Every step needs a subject.')
      return
    }
    if (draft.steps.some((s) => !s.bodyTemplate.trim() || s.bodyTemplate === '<br>')) {
      setError('Every step needs a message body.')
      return
    }
    if (draft.sendDaysMask === 0) {
      setError('Select at least one day of the week to send.')
      return
    }
    if (draft.sendWindowEnd <= draft.sendWindowStart) {
      setError('The send window end time must be after the start time.')
      return
    }

    for (let i = 0; i < draft.steps.length; i++) {
      const s = draft.steps[i]
      if (s.channel === 3) {
        if (s.subjectTemplate.length > 200) {
          setError(`Step ${i + 1}: LinkedIn subject must be 200 characters or less (${s.subjectTemplate.length} characters).`)
          return
        }
        const bodyLen = stripHtml(s.bodyTemplate).length
        if (bodyLen > 1300) {
          setError(`Step ${i + 1}: LinkedIn body must be 1300 characters or less (${bodyLen} characters). LinkedIn limits InMail to non-connections.`)
          return
        }
      }
    }

    try {
      if (isNew) {
        const created = await createSequence(draft).unwrap()
        onSaved(created.id)
      } else {
        await updateSequence({ id: sequenceId as number, ...draft }).unwrap()
        setSavedMessage('Sequence saved.')
      }
    } catch {
      setError('Could not save the sequence. Check the details and try again.')
    }
  }

  const onGenerate = async () => {
    if (!generateJobId) {
      setGenerateError('Select a job to generate content for.')
      return
    }
    setGenerateError(null)
    setGenerating(true)
    try {
      const content = await generateContent({
        jobId: Number(generateJobId),
        stepName: step.name,
        channel: isEmail ? 'Email' : 'LinkedIn',
        stepNumber: selectedStep + 1,
      }).unwrap()
      updateStep(selectedStep, { subjectTemplate: content.subject, bodyTemplate: content.body })
    } catch {
      setGenerateError('Could not generate content. Check the API key and try again.')
    } finally {
      setGenerating(false)
    }
  }

  const onPreview = async () => {
    if (!previewLeadId) {
      setError('Pick a lead to preview the personalisation.')
      return
    }
    setError(null)
    try {
      const result = await previewSequence({
        leadId: Number(previewLeadId),
        subjectTemplate: step.subjectTemplate,
        bodyTemplate: step.bodyTemplate,
        channel: step.channel,
        includeUnsubscribe: draft.includeUnsubscribe,
        jobId: generateJobId ? Number(generateJobId) : undefined,
      }).unwrap()
      setPreview(result)
    } catch {
      setError('Could not build the preview.')
    }
  }

  if (!isNew && isLoading) {
    return (
      <p className="flex items-center gap-2 text-sm text-muted-foreground">
        <Loader2 className="animate-spin" /> Loading sequence...
      </p>
    )
  }

  const saving = creating || updating

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center gap-3">
        <Button variant="ghost" size="icon" onClick={onBack} aria-label="Back to sequences">
          <ArrowLeft />
        </Button>
        <div className="flex-1">
          <Input
            value={draft.name}
            onChange={(e) => updateDraft({ name: e.target.value })}
            placeholder="Untitled sequence"
            className="h-10 border-transparent px-2 text-xl font-semibold shadow-none focus-visible:border-input"
          />
        </div>
        <Select
          className="w-36"
          value={String(draft.status)}
          onChange={(e) => updateDraft({ status: Number(e.target.value) })}
        >
          {Object.entries(sequenceStatusLabels).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </Select>
        <Button onClick={onSave} disabled={saving}>
          {saving ? <Loader2 className="animate-spin" /> : <Save />}
          Save sequence
        </Button>
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

      <div className="grid gap-6 lg:grid-cols-[17rem_1fr]">
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold">Steps</h3>
            <Badge variant="secondary">{draft.steps.length}</Badge>
          </div>
          <div className="space-y-2">
            {draft.steps.map((item, index) => (
              <button
                key={index}
                type="button"
                onClick={() => {
                  setSelectedStep(index)
                  setPreview(null)
                }}
                className={cn(
                  'w-full rounded-lg border px-3 py-2.5 text-left transition-colors',
                  selectedStep === index
                    ? 'border-primary bg-primary/5'
                    : 'hover:bg-accent',
                )}
              >
                <div className="flex items-center justify-between gap-2">
                  <span className="text-sm font-medium">{item.name}</span>
                  {draft.steps.length > 1 && (
                    <span
                      role="button"
                      tabIndex={0}
                      aria-label={`Remove ${item.name}`}
                      onClick={(e) => {
                        e.stopPropagation()
                        removeStep(index)
                      }}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.stopPropagation()
                          removeStep(index)
                        }
                      }}
                      className="text-muted-foreground hover:text-destructive"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </span>
                  )}
                </div>
                <p className="mt-0.5 truncate text-xs text-muted-foreground">
                  {item.subjectTemplate || 'No subject yet'}
                </p>
                {index > 0 && (
                  <p className="mt-1 text-[11px] text-muted-foreground">
                    +{item.delayDays}d · {item.channel === OutreachChannel.LinkedIn ? 'LinkedIn' : 'Email'}
                  </p>
                )}
              </button>
            ))}
          </div>
          <Button variant="outline" className="w-full" onClick={addStep}>
            <Plus />
            Add step
          </Button>
        </div>

        <div className="space-y-6">
          {step && (
            <Card>
              <CardHeader>
                <CardTitle>Step {selectedStep + 1}</CardTitle>
                <CardDescription>
                  {isEmail
                    ? 'Compose the email subject and body.'
                    : 'Compose the LinkedIn InMail subject and message.'}
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-1.5">
                    <Label>Step name</Label>
                    <Input
                      value={step.name}
                      onChange={(e) => updateStep(selectedStep, { name: e.target.value })}
                      placeholder="Opening email"
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label>Outreach channel</Label>
                    <Select
                      value={String(step.channel)}
                      onChange={(e) =>
                        updateStep(selectedStep, { channel: Number(e.target.value) })
                      }
                    >
                      <option value={OutreachChannel.Email}>
                        {outreachChannelLabels[OutreachChannel.Email]}
                      </option>
                      <option value={OutreachChannel.Sms}>
                        {outreachChannelLabels[OutreachChannel.Sms]}
                      </option>
                      <option value={OutreachChannel.LinkedIn}>
                        {outreachChannelLabels[OutreachChannel.LinkedIn]}
                      </option>
                    </Select>
                  </div>
                </div>

                <div className="space-y-1.5">
                  <Label>{isEmail ? 'Email subject' : 'LinkedIn subject'}</Label>
                  <Input
                    value={step.subjectTemplate}
                    onChange={(e) =>
                      updateStep(selectedStep, { subjectTemplate: e.target.value })
                    }
                    placeholder={isEmail ? 'Quick question, {{FirstName}}' : 'Quick question'}
                  />
                  {!isEmail && (
                    <p className={`text-xs ${step.subjectTemplate.length > 200 ? 'text-destructive' : 'text-muted-foreground'}`}>
                      {step.subjectTemplate.length}/200 characters
                    </p>
                  )}
                </div>

                <div className="rounded-md border bg-primary/5 p-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <Sparkles className="h-4 w-4 text-primary" />
                    <span className="text-sm font-medium">Generate with AI</span>
                    <Select
                      className="h-8 w-56 text-xs"
                      value={generateJobId}
                      onChange={(e) => setGenerateJobId(e.target.value)}
                    >
                      <option value="">Pick a job...</option>
                      {jobs.map((job) => (
                        <option key={job.id} value={job.id}>
                          {job.title}
                        </option>
                      ))}
                    </Select>
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      onClick={onGenerate}
                      disabled={generating || !generateJobId}
                    >
                      {generating ? <Loader2 className="animate-spin" /> : <Sparkles />}
                      Generate subject & body
                    </Button>
                  </div>
                  {generateError && (
                    <p className="mt-2 text-xs text-destructive">{generateError}</p>
                  )}
                  <p className="mt-1 text-xs text-muted-foreground">
                    Uses job and organization data to write the message. You can edit the result.
                  </p>
                </div>

                <div className="space-y-1.5">
                  <Label>Message body</Label>
                  <RichTextEditor
                    key={selectedStep}
                    value={step.bodyTemplate}
                    onChange={(html) => updateStep(selectedStep, { bodyTemplate: html })}
                    placeholder="Write your outreach message..."
                    attributes={attributeGroups}
                  />
                  {!isEmail && (
                    <p className={`text-xs ${stripHtml(step.bodyTemplate).length > 1300 ? 'text-destructive' : 'text-muted-foreground'}`}>
                      {stripHtml(step.bodyTemplate).length}/1300 characters (LinkedIn InMail limit for non-connections)
                    </p>
                  )}
                  <p className="text-xs text-muted-foreground">
                    Use <span className="font-medium">Insert attribute</span> to add contact details,
                    a job link, or an AI personalisation. Attributes are filled in per lead when the
                    message is sent.
                  </p>
                </div>

                {selectedStep > 0 && (
                  <div className="grid gap-4 rounded-md border bg-muted/30 p-3 sm:grid-cols-2">
                    <div className="space-y-1.5">
                      <Label>Wait before sending</Label>
                      <div className="flex items-center gap-2">
                        <Input
                          type="number"
                          min={0}
                          className="w-24"
                          value={step.delayDays}
                          onChange={(e) =>
                            updateStep(selectedStep, { delayDays: Number(e.target.value) })
                          }
                        />
                        <span className="text-sm text-muted-foreground">day(s) after the last step</span>
                      </div>
                    </div>
                    <div className="space-y-1.5">
                      <Label>Only send this step if</Label>
                      <Select
                        value={String(step.condition)}
                        onChange={(e) =>
                          updateStep(selectedStep, { condition: Number(e.target.value) })
                        }
                      >
                        {Object.entries(sequenceStepConditionLabels).map(([value, label]) => (
                          <option key={value} value={value}>
                            {label}
                          </option>
                        ))}
                      </Select>
                    </div>
                  </div>
                )}

                <div className="flex flex-wrap items-center gap-2 border-t pt-4">
                  <Select
                    className="h-8 w-64 text-xs"
                    value={previewLeadId}
                    onChange={(e) => setPreviewLeadId(e.target.value)}
                  >
                    <option value="">Preview with a lead...</option>
                    {leads.map((lead) => (
                      <option key={lead.id} value={lead.id}>
                        {lead.firstName} {lead.lastName}
                        {lead.company ? ` - ${lead.company}` : ''}
                      </option>
                    ))}
                  </Select>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={onPreview}
                    disabled={previewing}
                  >
                    {previewing ? <Loader2 className="animate-spin" /> : <Eye />}
                    Preview
                  </Button>
                </div>

                {preview && (
                  <div className="rounded-md border bg-muted/20 p-3">
                    <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                      Subject
                    </p>
                    <p className="mb-3 text-sm font-medium">{preview.subject || '(no subject)'}</p>
                    <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                      Body
                    </p>
                    <div
                      className="text-sm [&_a]:text-primary [&_a]:underline [&_ul]:list-disc [&_ul]:pl-5"
                      dangerouslySetInnerHTML={{ __html: preview.body }}
                    />
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          <Card>
            <CardHeader>
              <CardTitle>Sending schedule</CardTitle>
              <CardDescription>
                Choose the days and times follow-up emails can be sent. Steps that fall due outside
                this window are sent at the next available time.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-5">
              <div className="space-y-2">
                <Label>Days of the week</Label>
                <div className="flex flex-wrap gap-2">
                  {weekDays.map((day) => {
                    const active = (draft.sendDaysMask & day.bit) !== 0
                    return (
                      <button
                        key={day.bit}
                        type="button"
                        onClick={() => toggleDay(day.bit)}
                        className={cn(
                          'h-9 w-12 rounded-md border text-sm font-medium transition-colors',
                          active
                            ? 'border-primary bg-primary text-primary-foreground'
                            : 'hover:bg-accent',
                        )}
                      >
                        {day.label}
                      </button>
                    )
                  })}
                </div>
              </div>

              <div className="flex flex-wrap items-end gap-4">
                <div className="space-y-1.5">
                  <Label>From</Label>
                  <Input
                    type="time"
                    className="w-36"
                    value={draft.sendWindowStart}
                    onChange={(e) => updateDraft({ sendWindowStart: e.target.value })}
                  />
                </div>
                <div className="space-y-1.5">
                  <Label>To</Label>
                  <Input
                    type="time"
                    className="w-36"
                    value={draft.sendWindowEnd}
                    onChange={(e) => updateDraft({ sendWindowEnd: e.target.value })}
                  />
                </div>
                <p className="pb-2 text-xs text-muted-foreground">
                  {formatSendDays(draft.sendDaysMask)} at{' '}
                  {formatTimeWindow(draft.sendWindowStart, draft.sendWindowEnd)}
                </p>
              </div>

              <label className="flex items-start gap-2 rounded-md border bg-muted/30 p-3">
                <input
                  type="checkbox"
                  className="mt-0.5"
                  checked={draft.includeUnsubscribe}
                  onChange={(e) => updateDraft({ includeUnsubscribe: e.target.checked })}
                />
                <span className="text-sm">
                  <span className="font-medium">Include an unsubscribe link in every email.</span>
                  <span className="block text-xs text-muted-foreground">
                    A one-click unsubscribe footer is added automatically, and the List-Unsubscribe
                    header is set so Yahoo, Hotmail and Google do not flag the email as spam.
                  </span>
                </span>
              </label>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Sequence details</CardTitle>
              <CardDescription>Optional notes for this sequence.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-1.5">
                <Label>Description</Label>
                <Textarea
                  rows={2}
                  value={draft.description}
                  onChange={(e) => updateDraft({ description: e.target.value })}
                  placeholder="What is this sequence for?"
                />
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
