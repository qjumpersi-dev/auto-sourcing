import { useState } from 'react'
import { useDispatch } from 'react-redux'
import { Loader2, Save, Send, X } from 'lucide-react'
import {
  useChangePasswordMutation,
  useCreateExtensionCodeMutation,
  useDisconnectMicrosoftMutation,
  useGetMicrosoftStatusQuery,
  useLazyGetMicrosoftConnectUrlQuery,
  useSendTestEmailMutation,
  useUpdateMeMutation,
} from '@/services/apiSlice'
import { setUser } from '@/store/authSlice'
import type { AuthUser } from '@/types/models'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

export function AccountModal({ user, onClose }: { user: AuthUser; onClose: () => void }) {
  const dispatch = useDispatch()
  const [updateMe, { isLoading }] = useUpdateMeMutation()
  const [sendTestEmail, { isLoading: testing }] = useSendTestEmailMutation()
  const { data: microsoft, refetch: refetchMicrosoft } = useGetMicrosoftStatusQuery()
  const [fetchConnectUrl, { isLoading: connecting }] = useLazyGetMicrosoftConnectUrlQuery()
  const [disconnectMicrosoft, { isLoading: disconnecting }] = useDisconnectMicrosoftMutation()
  const [createExtensionCode, { isLoading: creatingCode }] = useCreateExtensionCodeMutation()
  const [extensionCode, setExtensionCode] = useState<string | null>(null)
  const [extensionError, setExtensionError] = useState<string | null>(null)
  const [changePassword, { isLoading: changingPassword }] = useChangePasswordMutation()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [passwordSaved, setPasswordSaved] = useState(false)

  const [displayName, setDisplayName] = useState(user.displayName)
  const [sendFromName, setSendFromName] = useState(user.sendFromName ?? '')
  const [sendFromAddress, setSendFromAddress] = useState(user.sendFromAddress ?? '')
  const [replyToAddress, setReplyToAddress] = useState(user.replyToAddress ?? '')
  const [testTo, setTestTo] = useState('')
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [testResult, setTestResult] = useState<{ ok: boolean; message: string } | null>(null)
  const [microsoftError, setMicrosoftError] = useState<string | null>(null)

  const onConnectMicrosoft = async () => {
    setMicrosoftError(null)
    try {
      const { url } = await fetchConnectUrl().unwrap()
      window.location.href = url
    } catch (err) {
      const data = (err as { data?: { error?: string } })?.data
      setMicrosoftError(data?.error ?? 'Could not start the Microsoft sign-in.')
    }
  }

  const onDisconnectMicrosoft = async () => {
    setMicrosoftError(null)
    try {
      await disconnectMicrosoft().unwrap()
      refetchMicrosoft()
    } catch {
      setMicrosoftError('Could not disconnect Microsoft 365.')
    }
  }

  const onGenerateExtensionCode = async () => {
    setExtensionError(null)
    try {
      const result = await createExtensionCode().unwrap()
      setExtensionCode(result.code)
    } catch {
      setExtensionError('Could not generate a code. Make sure you are signed in.')
    }
  }

  const onChangePassword = async () => {
    setPasswordError(null)
    setPasswordSaved(false)
    try {
      await changePassword({ currentPassword, newPassword }).unwrap()
      setPasswordSaved(true)
      setCurrentPassword('')
      setNewPassword('')
    } catch (err) {
      const data = (err as { data?: { error?: string } })?.data
      setPasswordError(data?.error ?? 'Could not change your password.')
    }
  }

  const buildPayload = () => ({
    displayName,
    sendFromName: sendFromName || null,
    sendFromAddress: sendFromAddress || null,
    replyToAddress: replyToAddress || null,
  })

  const onSave = async () => {
    setError(null)
    setSaved(false)
    try {
      const updated = await updateMe(buildPayload()).unwrap()
      dispatch(setUser(updated))
      setSaved(true)
    } catch {
      setError('Could not save your account settings.')
    }
  }

  const onTest = async () => {
    setError(null)
    setSaved(false)
    setTestResult(null)
    try {
      const updated = await updateMe(buildPayload()).unwrap()
      dispatch(setUser(updated))

      const result = await sendTestEmail({ to: testTo || undefined }).unwrap()
      setTestResult(
        result.sent
          ? { ok: true, message: `Test email sent to ${result.to}. Check that inbox (and spam).` }
          : { ok: false, message: result.error ?? 'The server could not send the test email.' },
      )
    } catch (err) {
      const e = err as { status?: number | string; data?: { error?: string }; error?: string }
      const detail = e?.data?.error ?? e?.error
      setTestResult({
        ok: false,
        message: detail
          ? `Could not send the test email: ${detail}`
          : `Could not send the test email (status: ${e?.status ?? 'network error'}).`,
      })
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-lg border bg-white p-6 shadow-xl">
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

          <div className="rounded-md border p-3">
            <p className="text-sm font-medium">Microsoft 365 sending</p>
            {microsoft?.connected ? (
              <>
                <p className="mt-1 text-xs text-muted-foreground">
                  Connected as <span className="font-medium">{microsoft.accountEmail}</span>. Emails you send
                  go out from this mailbox.
                </p>
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-2"
                  onClick={onDisconnectMicrosoft}
                  disabled={disconnecting}
                >
                  {disconnecting ? <Loader2 className="animate-spin" /> : null}
                  Disconnect
                </Button>
              </>
            ) : (
              <>
                <p className="mt-1 text-xs text-muted-foreground">
                  Connect your Microsoft 365 account so outreach is sent from your own mailbox (no password
                  needed).
                </p>
                {microsoft && !microsoft.configured && (
                  <p className="mt-1 text-xs text-destructive">
                    Microsoft 365 isn't configured on the server yet.
                  </p>
                )}
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-2"
                  onClick={onConnectMicrosoft}
                  disabled={connecting || microsoft?.configured === false}
                >
                  {connecting ? <Loader2 className="animate-spin" /> : null}
                  Connect Microsoft 365
                </Button>
              </>
            )}
            {microsoftError && <p className="mt-2 text-sm text-destructive">{microsoftError}</p>}
          </div>

          <div className="rounded-md border p-3">
            <p className="text-sm font-medium">LinkedIn extension</p>
            <p className="mt-1 text-xs text-muted-foreground">
              Install the <strong>AITS LinkedIn Sender</strong> browser extension, then paste a connect code
              into it to link your LinkedIn session. Queued InMails are then sent from your own browser.
            </p>
            {extensionCode && (
              <div className="mt-2">
                <p className="text-xs text-muted-foreground">Connect code (valid for 10 minutes):</p>
                <p className="font-mono text-lg font-semibold tracking-[0.2em]">{extensionCode}</p>
              </div>
            )}
            <Button
              variant="outline"
              size="sm"
              className="mt-2"
              onClick={onGenerateExtensionCode}
              disabled={creatingCode}
            >
              {creatingCode ? <Loader2 className="animate-spin" /> : null}
              {extensionCode ? 'Generate a new code' : 'Generate connect code'}
            </Button>
            {extensionError && <p className="mt-2 text-sm text-destructive">{extensionError}</p>}
          </div>

          <div className="rounded-md border p-3">
            <p className="text-sm font-medium">Change password</p>
            <div className="mt-2 grid gap-3 sm:grid-cols-2">
              <div className="space-y-1.5">
                <Label>Current password</Label>
                <Input
                  type="password"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  autoComplete="current-password"
                />
              </div>
              <div className="space-y-1.5">
                <Label>New password</Label>
                <Input
                  type="password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  placeholder="At least 8 characters"
                  autoComplete="new-password"
                />
              </div>
            </div>
            {passwordError && <p className="mt-2 text-sm text-destructive">{passwordError}</p>}
            {passwordSaved && <p className="mt-2 text-sm text-emerald-600">Password changed.</p>}
            <Button
              variant="outline"
              size="sm"
              className="mt-2"
              onClick={onChangePassword}
              disabled={changingPassword || !currentPassword || newPassword.length < 8}
            >
              {changingPassword ? <Loader2 className="animate-spin" /> : null}
              Change password
            </Button>
          </div>

          <div className="rounded-md border p-3">
            <p className="text-sm font-medium">Test your email setup</p>
            <p className="text-xs text-muted-foreground">
              Saves your settings, then sends a test message so you can confirm delivery.
            </p>
            <div className="mt-2 flex items-end gap-2">
              <div className="flex-1 space-y-1.5">
                <Label>Send to</Label>
                <Input
                  type="email"
                  value={testTo}
                  onChange={(e) => setTestTo(e.target.value)}
                  placeholder={user.email}
                />
              </div>
              <Button variant="outline" onClick={onTest} disabled={testing || isLoading}>
                {testing ? <Loader2 className="animate-spin" /> : <Send />}
                Send test email
              </Button>
            </div>
            {testResult && (
              <p
                className={
                  testResult.ok
                    ? 'mt-2 text-sm text-emerald-600'
                    : 'mt-2 text-sm text-destructive'
                }
              >
                {testResult.message}
              </p>
            )}
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
