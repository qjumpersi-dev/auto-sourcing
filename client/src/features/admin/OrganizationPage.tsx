import { useEffect, useState } from 'react'
import { Loader2, Save } from 'lucide-react'
import { useGetOrganizationQuery, useUpdateOrganizationMutation } from '@/services/apiSlice'
import { RichTextEditor } from '@/components/RichTextEditor'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

interface OrgDraft {
  orgName: string
  about: string
  evp: string
  culture: string
  hiringProcess: string
  eeo: string
  guardRails: string
}

const sections: { key: keyof OrgDraft; label: string; description: string }[] = [
  {
    key: 'about',
    label: 'About the company',
    description: 'Overview of the company for AI agent conversations.',
  },
  {
    key: 'evp',
    label: 'EVP / Why join / Office details',
    description: 'Employee value proposition, what makes working here attractive, and office details.',
  },
  {
    key: 'culture',
    label: 'Culture and benefits',
    description: 'Company culture, benefits, and what it is like to work here.',
  },
  {
    key: 'hiringProcess',
    label: 'Hiring process',
    description: 'How the company hires — stages, timelines, and what to expect.',
  },
  {
    key: 'eeo',
    label: 'EEO / Community work',
    description: 'Equal employment opportunity, diversity, and community involvement.',
  },
  {
    key: 'guardRails',
    label: 'Guard rails',
    description:
      'Triggers where the AI agent must suggest the candidate speak to a human. Include specific topics or conditions.',
  },
]

export function OrganizationPage() {
  const { data: profile, isLoading } = useGetOrganizationQuery()
  const [updateProfile, { isLoading: saving }] = useUpdateOrganizationMutation()

  const [draft, setDraft] = useState<OrgDraft>({
    orgName: '',
    about: '',
    evp: '',
    culture: '',
    hiringProcess: '',
    eeo: '',
    guardRails: '',
  })
  const [initialised, setInitialised] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (profile && !initialised) {
      setDraft({
        orgName: profile.orgName ?? '',
        about: profile.about ?? '',
        evp: profile.evp ?? '',
        culture: profile.culture ?? '',
        hiringProcess: profile.hiringProcess ?? '',
        eeo: profile.eeo ?? '',
        guardRails: profile.guardRails ?? '',
      })
      setInitialised(true)
    }
  }, [profile, initialised])

  const updateField = (key: keyof OrgDraft, value: string) => {
    setDraft((current) => ({ ...current, [key]: value }))
    setSaved(false)
  }

  const onSave = async () => {
    setError(null)
    setSaved(false)
    try {
      await updateProfile({
        orgName: draft.orgName,
        about: draft.about,
        evp: draft.evp,
        culture: draft.culture,
        hiringProcess: draft.hiringProcess,
        eeo: draft.eeo,
        guardRails: draft.guardRails,
      }).unwrap()
      setSaved(true)
    } catch (e) {
      const data = (e as { data?: { error?: string } })?.data
      setError(data?.error ?? 'Could not save the organization profile.')
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
          <h2 className="text-xl font-semibold">Organization profile</h2>
          <p className="text-sm text-muted-foreground">
            AI agent grounding data. These sections are used by the AI agent to answer candidate
            questions and generate outreach content. Links are not used — use plain text and images
            only.
          </p>
        </div>
        <Button onClick={onSave} disabled={saving}>
          {saving ? <Loader2 className="animate-spin" /> : <Save />}
          Save profile
        </Button>
      </div>

      {error && (
        <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}
      {saved && (
        <p className="rounded-md border border-emerald-300 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
          Organization profile saved.
        </p>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Organization name</CardTitle>
          <CardDescription>
            The name of your organization. Used as {'{{OrgName}}'} in outreach templates.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-1.5">
            <Label>Organization name</Label>
            <Input
              value={draft.orgName}
              onChange={(e) => updateField('orgName', e.target.value)}
              placeholder="e.g. QJumpers"
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
              placeholder={`Write about ${section.label.toLowerCase()}...`}
              showLink={false}
            />
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
