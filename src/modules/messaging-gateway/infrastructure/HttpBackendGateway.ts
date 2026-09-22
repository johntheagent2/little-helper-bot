import axios, { type AxiosInstance } from "axios";
import type {
  BackendGateway,
  BackendMessageRequest,
  BackendMessageResponse,
} from "../domain/BackendGateway.js";

export interface HttpBackendGatewayOptions {
  baseUrl: string;
  apiKey?: string;
}

// Hand-rolled request/response shapes for now — the .NET backend doesn't
// exist yet, so there's no Swagger doc to generate against. Once it does,
// this is the one file that should start importing from src/generated/
// (see docs/analysis-phase.md, contract-first decision) instead of the
// types in ../domain/BackendGateway.ts.
export class HttpBackendGateway implements BackendGateway {
  private readonly http: AxiosInstance;

  constructor(options: HttpBackendGatewayOptions) {
    this.http = axios.create({
      baseURL: options.baseUrl,
      timeout: 5_000,
      headers: options.apiKey ? { Authorization: `Bearer ${options.apiKey}` } : undefined,
    });
  }

  async handleMessage(request: BackendMessageRequest): Promise<BackendMessageResponse> {
    const { data } = await this.http.post<BackendMessageResponse>("/messages", request);
    return data;
  }
}
