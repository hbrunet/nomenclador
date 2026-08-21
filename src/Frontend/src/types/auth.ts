export interface LoginDto {
  username: string
  password: string
}

export interface LoginResultDto {
  tokenType: string
  expiresAt: string
  displayName: string
}
