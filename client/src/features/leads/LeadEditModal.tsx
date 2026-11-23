import { useState } from 'react'
import { Loader2, Save, X } from 'lucide-react'
import { useUpdateLeadMutation } from '@/services/apiSlice'
import type { Lead } from '@/types/models'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { LookupField } from '@/components/LookupField'

interface EditDraft {
  firstName: string
  lastName: string
  email: string
  phone: string
  linkedInUrl: string
  company: string
  jobTitle: string
  location: string
  country: string
  industryTag: string
  jobTitleTag: string
  roleFamilyTag: string
  sourceIndustry: string
  payExpectation: string
  availability: string
  noticePeriod: string
  workAuthorisation: string
  licencesHeld: string
}

export function LeadEditModal({ lead, onClose }: { lead: Lead; onClose: () => void }) {
  const [updateLead, { isLoading: saving }] = useUpdateLeadMutation()
  const [draft, setDraft] = useState<EditDraft>({
    firstName: lead.firstName,
    lastName: lead.lastName,
    email: lead.email,
    phone: lead.phone ?? '',
    linkedInUrl: lead.linkedInUrl ?? '',
    company: lead.company ?? '',
    jobTitle: lead.jobTitle ?? '',
    location: lead.location ?? '',
    country: lead.country ?? '',
    industryTag: lead.industryTag ?? '',
    jobTitleTag: lead.jobTitleTag ?? '',
    roleFamilyTag: lead.roleFamilyTag ?? '',
    sourceIndustry: lead.sourceIndustry ?? '',
    payExpectation: lead.payExpectation ?? '',
    availability: lead.availability ?? '',
    noticePeriod: lead.noticePeriod ?? '',
    workAuthorisation: lead.workAuthorisation ?? '',
    licencesHeld: lead.licencesHeld ?? '',
  })
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  const update = (patch: Partial<EditDraft>) => {
    setDraft((current) => ({ ...current, ...patch }))
    setSaved(false)
  }

  const onSave = async () => {
    setError(null)
    setSaved(false)
    try {
      await updateLead({
        id: lead.id,
        firstName: draft.firstName,
        lastName: draft.lastName,
        email: draft.email,
        phone: draft.phone || null,
        linkedInUrl: draft.linkedInUrl || null,
        company: draft.company || null,
        jobTitle: draft.jobTitle || null,
        location: draft.location || null,
        country: draft.country || null,
        industryTag: draft.industryTag || null,
        jobTitleTag: draft.jobTitleTag || null,
        roleFamilyTag: draft.roleFamilyTag || null,
        sourceIndustry: draft.sourceIndustry || null,
        payExpectation: draft.payExpectation || null,
        availability: draft.availability || null,
        noticePeriod: draft.noticePeriod || null,
        workAuthorisation: draft.workAuthorisation || null,
        licencesHeld: draft.licencesHeld || null,
      }).unwrap()
      setSaved(true)
    } catch {
      setError('Could not save the contact details.')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-lg border bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h3 className="text-lg font-semibold">Edit contact details</h3>
          <Button variant="ghost" size="icon" onClick={onClose} aria-label="Close">
            <X />
          </Button>
        </div>

        <div className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label>First name</Label>
              <Input
                value={draft.firstName}
                onChange={(e) => update({ firstName: e.target.value })}
              />
            </div>
            <div className="space-y-1.5">
              <Label>Last name</Label>
              <Input
                value={draft.lastName}
                onChange={(e) => update({ lastName: e.target.value })}
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label>Email</Label>
            <Input
              value={draft.email}
              onChange={(e) => update({ email: e.target.value })}
              placeholder="name@example.com"
            />
          </div>

          <div className="space-y-1.5">
            <Label>Mobile phone</Label>
            <Input
              value={draft.phone}
              onChange={(e) => update({ phone: e.target.value })}
              placeholder="+64 21 123 4567"
            />
          </div>

          <div className="space-y-1.5">
            <Label>LinkedIn URL</Label>
            <Input
              value={draft.linkedInUrl}
              onChange={(e) => update({ linkedInUrl: e.target.value })}
              placeholder="https://www.linkedin.com/in/..."
            />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label>Company</Label>
              <Input
                value={draft.company}
                onChange={(e) => update({ company: e.target.value })}
              />
            </div>
            <div className="space-y-1.5">
              <Label>Job title</Label>
              <Input
                value={draft.jobTitle}
                onChange={(e) => update({ jobTitle: e.target.value })}
              />
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <Label>Location</Label>
              <Input
                value={draft.location}
                onChange={(e) => update({ location: e.target.value })}
              />
            </div>
            <div className="space-y-1.5">
              <Label>Country</Label>
              <Input
                value={draft.country}
                onChange={(e) => update({ country: e.target.value })}
              />
            </div>
          </div>

          <div className="space-y-3 rounded-md border p-3">
            <p className="text-sm font-medium">Talent pool tags</p>
            <div className="space-y-1.5">
              <Label>Industry tag</Label>
              <LookupField
                category="industry"
                value={draft.industryTag}
                onChange={(value) => update({ industryTag: value })}
                placeholder="Select or add an industry"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Role family tag</Label>
              <LookupField
                category="role-family"
                value={draft.roleFamilyTag}
                onChange={(value) => update({ roleFamilyTag: value })}
                placeholder="Select or add a role family"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Job title tag</Label>
              <LookupField
                category="job-title"
                value={draft.jobTitleTag}
                onChange={(value) => update({ jobTitleTag: value })}
                placeholder="Select or add a job title"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Source industry (where they work now)</Label>
              <Input
                value={draft.sourceIndustry}
                onChange={(e) => update({ sourceIndustry: e.target.value })}
                placeholder="e.g. Telecommunications"
              />
            </div>
          </div>

          <div className="space-y-3 rounded-md border p-3">
            <p className="text-sm font-medium">Candidate details for reuse</p>
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label>Pay expectation</Label>
                <Input
                  value={draft.payExpectation}
                  onChange={(e) => update({ payExpectation: e.target.value })}
                  placeholder="e.g. USD 95,000"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Availability</Label>
                <Input
                  value={draft.availability}
                  onChange={(e) => update({ availability: e.target.value })}
                  placeholder="e.g. immediately"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Notice period</Label>
                <Input
                  value={draft.noticePeriod}
                  onChange={(e) => update({ noticePeriod: e.target.value })}
                  placeholder="e.g. 4 weeks"
                />
              </div>
              <div className="space-y-1.5">
                <Label>Work authorisation</Label>
                <Input
                  value={draft.workAuthorisation}
                  onChange={(e) => update({ workAuthorisation: e.target.value })}
                  placeholder="e.g. citizen / visa / needs sponsorship"
                />
              </div>
            </div>
            <div className="space-y-1.5">
              <Label>Licences held</Label>
              <Input
                value={draft.licencesHeld}
                onChange={(e) => update({ licencesHeld: e.target.value })}
                placeholder="e.g. state electrical licence"
              />
            </div>
          </div>

          {error && <p className="text-xs text-destructive">{error}</p>}
          {saved && (
            <p className="text-xs text-emerald-600">Contact details saved.</p>
          )}

          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={onClose}>
              Close
            </Button>
            <Button onClick={onSave} disabled={saving}>
              {saving ? <Loader2 className="animate-spin" /> : <Save />}
              Save
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
