import { useState } from 'react'
import { Loader2, Save, X } from 'lucide-react'
import { useUpdateLeadMutation } from '@/services/apiSlice'
import type { Lead } from '@/types/models'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

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
      }).unwrap()
      setSaved(true)
    } catch {
      setError('Could not save the contact details.')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="w-full max-w-lg rounded-lg border bg-white p-6 shadow-xl">
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
