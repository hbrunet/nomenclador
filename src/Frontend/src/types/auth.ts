export interface LoginDto {
  username: string
  password: string
}

export interface LoginResultDto {
  token: string
  tokenType: string
  expiresAt: string
  displayName: string
}
