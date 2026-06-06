export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  phoneNumber?: string | null;
}

export interface LoginResult {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  avatarUrl?: string | null;
  accessToken: string;
  expiresAtUtc: string;
}

export interface AuthSession {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  avatarUrl?: string | null;
  accessToken: string;
  expiresAtUtc: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}

