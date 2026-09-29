import { useEffect, useState } from 'react'
import { Loader2, Save, ShieldCheck } from 'lucide-react'
import { useGetPolicyQuery, useUpdatePolicyMutation } from '@/services/apiSlice'
import { RichTextEditor } from '@/components/RichTextEditor'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

interface PolicyDraft {
  whatAiMayAnswer: string
  escalationTriggers: string
  refusalTopics: string
  requiredDisclaimers: string
  marketRules: string
  needsHumanStates: string
  confidenceThreshold: string
}

const sections: { key: keyof PolicyDraft; label: string; description: string }[] = [
  {
    key: 'whatAiMayAnswer',
    label: 'What the AI may answer',
    description: 'Approved topics the AI agent can respond to directly.',
  },
  {
    key: 'escalationTriggers',
    label: 'What it must hand to a recruiter',
    description: 'Triggers where the AI must escalate to a human recruiter.',
  },
  {
    key: 'refusalTopics',
    label: 'Topics it should avoid',
    description: 'Topics the AI must refuse or deflect.',
  },
  {
    key: 'requiredDisclaimers',
    label: 'Required disclaimers',
    description: 'Disclaimers the AI must include in certain responses.',
  },
  {
    key: 'marketRules',
    label: 'Market / geography rules',
    description: 'Rules that vary by market or geography.',
  },
  {
    key: 'needsHumanStates',
    label: 'Needs human states',
    description: 'States or conditions that require a human to take over.',
  },
]

export function GuardrailsPage() {
  const { data: policy, isLoading } = useGetPolicyQuery()
  const [updatePolicy, { isLoading: saving }] = useUpdatePolicyMutation()

  const [draft, setDraft] = useState<PolicyDraft>({
    whatAiMayAnswer: '',
    escalationTriggers: '',
    refusalTopics: '',
    requiredDisclaimers: '',
    marketRules: '',
    needsHumanStates: '',
    confidenceThreshold: '',
  })
  const [initialised, setInitialised] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (policy && !initialised) {
      setDraft({
        whatAiMayAnswer: policy.whatAiMayAnswer ?? '',
        escalationTriggers: policy.escalationTriggers ?? '',
        refusalTopics: policy.refusalTopics ?? '',
        requiredDisclaimers: policy.requiredDisclaimers ?? '',
        marketRules: policy.marketRules ?? '',
        needsHumanStates: policy.needsHumanStates ?? '',
        confidenceThreshold: policy.confidenceThreshold != null ? String(policy.confidenceThreshold) : '',
      })
      setInitialised(true)
    }
  }, [policy, initialised])

  const updateField = (key: keyof PolicyDraft, value: string) => {
    setDraft((current) => ({ ...current, [key]: value }))
    setSaved(false)
  }

  const onSave = async () => {
    setError(null)
    setSaved(false)
    try {
      await updatePolicy({
        whatAiMayAnswer: draft.whatAiMayAnswer,
        escalationTriggers: draft.escalationTriggers,
        refusalTopics: draft.refusalTopics,
        requiredDisclaimers: draft.requiredDisclaimers,
        marketRules: draft.marketRules,
        needsHumanStates: draft.needsHumanStates,
        confidenceThreshold: draft.confidenceThreshold ? Number(draft.confidenceThreshold) : null,
      }).unwrap()
      setSaved(true)
    } catch {
      setError('Could not save the guardrails.')
    }
  }

  if (isLoading) {
    return (
      <p className="flex items-center gap-2 text-sm text-muted-foreground">
        <Loader2 className="animate-spin" /> Loading...
      </p>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h2 className="flex items-center gap-2 text-xl font-semibold">
            <ShieldCheck className="h-5 w-5 text-blue-600" />
            Policy &amp; guardrails
          </h2>
          <p className="text-sm text-muted-foreground">
            Guidelines that apply to all jobs and campaigns. These rules control what the AI agent
            may answer, when it must escalate to a human, and what it must avoid.
          </p>
        </div>
        <Button onClick={onSave} disabled={saving}>
          {saving ? <Loader2 className="animate-spin" /> : <Save />}
          Save guardrails
        </Button>
      </div>

      {error && (
        <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}
      {saved && (
        <p className="rounded-md border border-emerald-300 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
          Guardrails saved.
        </p>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Confidence threshold</CardTitle>
          <CardDescription>
            The minimum confidence score (0–100) before the AI escalates to a human.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="max-w-xs space-y-1.5">
            <Label>Confidence threshold (%)</Label>
            <Input
              type="number"
              min={0}
              max={100}
              value={draft.confidenceThreshold}
              onChange={(e) => updateField('confidenceThreshold', e.target.value)}
              placeholder="e.g. 80"
            />
          </div>
        </CardContent>
      </Card>

      {sections.map((section) => (
        <Card key={section.key}>
          <CardHeader>
            <CardTitle>{section.label}</CardTitle>
            <CardDescription>{section.description}</CardDescription>
          </CardHeader>
          <CardContent>
            <RichTextEditor
              value={draft[section.key]}
              onChange={(html) => updateField(section.key, html)}
              placeholder={`Write ${section.label.toLowerCase()}...`}
              showLink={false}
            />
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
