import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import type { AuthUser } from '@/types/models'

export const AUTH_TOKEN_KEY = 'aits_token'

interface AuthState {
  token: string | null
  user: AuthUser | null
}

const initialState: AuthState = {
  token: typeof localStorage !== 'undefined' ? localStorage.getItem(AUTH_TOKEN_KEY) : null,
  user: null,
}

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setCredentials(state, action: PayloadAction<{ token: string; user: AuthUser }>) {
      state.token = action.payload.token
      state.user = action.payload.user
      localStorage.setItem(AUTH_TOKEN_KEY, action.payload.token)
    },
    setUser(state, action: PayloadAction<AuthUser>) {
      state.user = action.payload
    },
    clearCredentials(state) {
      state.token = null
      state.user = null
      localStorage.removeItem(AUTH_TOKEN_KEY)
    },
  },
})

export const { setCredentials, setUser, clearCredentials } = authSlice.actions
export default authSlice.reducer
