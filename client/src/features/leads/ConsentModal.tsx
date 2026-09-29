import { useState } from 'react'
import { Loader2, X } from 'lucide-react'
import {
  useGetLeadConsentQuery,
  useUpdateLeadConsentMutation,
  useSetPreferredChannelMutation,
} from '@/services/apiSlice'
import {
  ConsentChannel,
  consentChannelLabels,
  ConsentStatus,
  consentStatusLabels,
} from '@/types/models'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'

export function ConsentModal({ leadId, onClose }: { leadId: number; onClose: () => void }) {
  const { data: consent, isLoading } = useGetLeadConsentQuery(leadId)
  const [updateConsent] = useUpdateLeadConsentMutation()
  const [setPreferredChannel] = useSetPreferredChannelMutation()
  const [saving, setSaving] = useState(false)

  const onSetConsent = async (channel: number, status: number) => {
    setSaving(true)
    try {
      await updateConsent({ leadId, channel, status }).unwrap()
    } finally {
      setSaving(false)
    }
  }

  const onSetPreferred = async (channel: number | null) => {
    setSaving(true)
    try {
      await setPreferredChannel({ leadId, channel }).unwrap()
    } finally {
      setSaving(false)
    }
  }

  const getConsentStatus = (channel: number): number => {
    return consent?.consents.find((c) => c.channel === channel)?.status ?? ConsentStatus.Unknown
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="w-full max-w-lg rounded-lg border bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-center justify-between">
          <h3 className="text-lg font-semibold">Channel consent</h3>
          <Button variant="ghost" size="icon" onClick={onClose} aria-label="Close">
            <X />
          </Button>
        </div>

        {isLoading ? (
          <p className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="animate-spin" /> Loading...
          </p>
        ) : (
          <div className="space-y-4">
            <div className="space-y-1.5">
              <Label>Preferred channel</Label>
              <Select
                value={consent?.preferredChannel !== null && consent?.preferredChannel !== undefined ? String(consent.preferredChannel) : ''}
                onChange={(e) => onSetPreferred(e.target.value ? Number(e.target.value) : null)}
              >
                <option value="">Not set</option>
                {Object.entries(consentChannelLabels).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </Select>
            </div>

            <div className="space-y-2">
              {[ConsentChannel.Email, ConsentChannel.Sms, ConsentChannel.LinkedIn].map((channel) => {
                const status = getConsentStatus(channel)
                return (
                  <div key={channel} className="flex items-center gap-3 rounded-md border p-2">
                    <span className="w-24 text-sm font-medium">{consentChannelLabels[channel]}</span>
                    <Select
                      className="h-8 w-40 text-xs"
                      value={String(status)}
                      onChange={(e) => onSetConsent(channel, Number(e.target.value))}
                    >
                      {Object.entries(consentStatusLabels).map(([val, label]) => (
                        <option key={val} value={val}>
                          {label}
                        </option>
                      ))}
                    </Select>
                    {channel === ConsentChannel.Sms && (
                      <span className="text-xs text-muted-foreground">Requires opt-in</span>
                    )}
                  </div>
                )
              })}
            </div>

            <p className="text-xs text-muted-foreground">
              Email and LinkedIn are allowed by default. SMS and WhatsApp require explicit opt-in.
              Opted-out and Do-not-contact candidates are excluded from all future outreach.
            </p>

            {saving && (
              <p className="flex items-center gap-2 text-xs text-muted-foreground">
                <Loader2 className="animate-spin" /> Saving...
              </p>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
