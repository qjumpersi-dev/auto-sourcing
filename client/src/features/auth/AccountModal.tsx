import { useState } from 'react'
import { useDispatch } from 'react-redux'
import { Loader2, Save, X } from 'lucide-react'
import { useUpdateMeMutation } from '@/services/apiSlice'
import { setUser } from '@/store/authSlice'
import type { AuthUser } from '@/types/models'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

export function AccountModal({ user, onClose }: { user: AuthUser; onClose: () => void }) {
  const dispatch = useDispatch()
  const [updateMe, { isLoading }] = useUpdateMeMutation()

  const [displayName, setDisplayName] = useState(user.displayName)
  const [sendFromName, setSendFromName] = useState(user.sendFromName ?? '')
  const [sendFromAddress, setSendFromAddress] = useState(user.sendFromAddress ?? '')
  const [replyToAddress, setReplyToAddress] = useState(user.replyToAddress ?? '')
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const onSave = async () => {
    setError(null)
    setSaved(false)
    try {
      const updated = await updateMe({
        displayName,
        sendFromName: sendFromName || null,
        sendFromAddress: sendFromAddress || null,
        replyToAddress: replyToAddress || null,
      }).unwrap()
      dispatch(setUser(updated))
      setSaved(true)
    } catch {
      setError('Could not save your account settings.')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="w-full max-w-lg rounded-lg border bg-white p-6 shadow-xl">
        <div className="mb-4 flex items-start justify-between">
          <div>
            <h3 className="text-lg font-semibold">Account &amp; sending identity</h3>
            <p className="text-sm text-muted-foreground">
              Emails you send will use these details.
            </p>
          </div>
          <Button variant="ghost" size="icon" onClick={onClose} aria-label="Close">
            <X />
          </Button>
        </div>

        <div className="space-y-4">
          <div className="space-y-1.5">
            <Label>Your name</Label>
            <Input value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
          </div>

          <div className="space-y-1.5">
            <Label>Sending display name</Label>
            <Input
              value={sendFromName}
              onChange={(e) => setSendFromName(e.target.value)}
              placeholder={user.displayName}
            />
            <p className="text-xs text-muted-foreground">
              The name recipients see. Defaults to your name.
            </p>
          </div>

          <div className="space-y-1.5">
            <Label>From address</Label>
            <Input
              type="email"
              value={sendFromAddress}
              onChange={(e) => setSendFromAddress(e.target.value)}
              placeholder={user.email}
            />
            <p className="text-xs text-muted-foreground">
              Leave blank to use the platform sending address. Your email provider must authorise any
              address you enter here.
            </p>
          </div>

          <div className="space-y-1.5">
            <Label>Reply-to address</Label>
            <Input
              type="email"
              value={replyToAddress}
              onChange={(e) => setReplyToAddress(e.target.value)}
              placeholder={user.email}
            />
            <p className="text-xs text-muted-foreground">
              Where candidate replies go. Defaults to your email.
            </p>
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}
          {saved && <p className="text-sm text-emerald-600">Saved.</p>}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={onClose}>
              Close
            </Button>
            <Button onClick={onSave} disabled={isLoading}>
              {isLoading ? <Loader2 className="animate-spin" /> : <Save />}
              Save
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
