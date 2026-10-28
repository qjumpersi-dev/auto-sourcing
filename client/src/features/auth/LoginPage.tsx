import { useState, type FormEvent } from 'react'
import { useDispatch } from 'react-redux'
import { Loader2, Lock } from 'lucide-react'
import { useGetAuthStatusQuery, useLoginMutation, useSetupAdminMutation } from '@/services/apiSlice'
import { setCredentials } from '@/store/authSlice'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

export function LoginPage() {
  const dispatch = useDispatch()
  const { data: status, isLoading, isError } = useGetAuthStatusQuery()
  const [login, { isLoading: loggingIn }] = useLoginMutation()
  const [setup, { isLoading: settingUp }] = useSetupAdminMutation()

  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  const needsSetup = status?.hasUsers === false
  const busy = loggingIn || settingUp

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    try {
      const result = needsSetup
        ? await setup({ email, displayName, password }).unwrap()
        : await login({ email, password }).unwrap()
      dispatch(setCredentials({ token: result.token, user: result.user }))
    } catch (err) {
      const data = (err as { data?: { error?: string } })?.data
      setError(data?.error ?? 'Could not sign in.')
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/30 p-6">
      <div className="w-full max-w-md rounded-lg border bg-white p-6 shadow-sm">
        <div className="mb-6 flex items-center gap-3">
          <img src="/qjumpers-logo.png" alt="QJumpers" className="h-8 w-auto" />
          <div>
            <h1 className="text-lg font-semibold">AI Sourcing &amp; Recruiting</h1>
            <p className="text-xs text-muted-foreground">
              {needsSetup ? 'Create the first administrator account' : 'Sign in to continue'}
            </p>
          </div>
        </div>

        {isLoading && (
          <p className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="animate-spin" /> Loading...
          </p>
        )}

        {isError && (
          <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
            Could not reach the API. Check that it is running.
          </p>
        )}

        {!isLoading && !isError && (
          <form className="space-y-4" onSubmit={onSubmit}>
            {needsSetup && (
              <div className="space-y-1.5">
                <Label>Your name</Label>
                <Input
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  placeholder="e.g. Simon Oldham"
                  required
                />
              </div>
            )}

            <div className="space-y-1.5">
              <Label>Email</Label>
              <Input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@yourcompany.com"
                autoComplete="username"
                required
              />
            </div>

            <div className="space-y-1.5">
              <Label>Password</Label>
              <Input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder={needsSetup ? 'Choose a password (min 8 characters)' : 'Your password'}
                autoComplete={needsSetup ? 'new-password' : 'current-password'}
                minLength={needsSetup ? 8 : undefined}
                required
              />
            </div>

            {error && <p className="text-sm text-destructive">{error}</p>}

            <Button type="submit" className="w-full" disabled={busy}>
              {busy ? <Loader2 className="animate-spin" /> : <Lock />}
              {needsSetup ? 'Create account & sign in' : 'Sign in'}
            </Button>

            {needsSetup && (
              <p className="text-xs text-muted-foreground">
                This creates the first administrator. You can add the sending identity after signing in.
              </p>
            )}
          </form>
        )}
      </div>
    </div>
  )
}
