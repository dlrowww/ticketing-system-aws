{{/*
Chart and resource names.
*/}}
{{- define "ticketing-system.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "ticketing-system.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{- define "ticketing-system.frontendName" -}}
{{- printf "%s-frontend" (include "ticketing-system.fullname" .) | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "ticketing-system.apiName" -}}
{{- printf "%s-api" (include "ticketing-system.fullname" .) | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "ticketing-system.apiServiceName" -}}
{{- default (include "ticketing-system.apiName" .) .Values.api.service.name | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "ticketing-system.labels" -}}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | quote }}
app.kubernetes.io/part-of: {{ include "ticketing-system.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}

{{- define "ticketing-system.frontendSelectorLabels" -}}
app.kubernetes.io/name: {{ include "ticketing-system.frontendName" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: frontend
{{- end }}

{{- define "ticketing-system.apiSelectorLabels" -}}
app.kubernetes.io/name: {{ include "ticketing-system.apiName" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/component: api
{{- end }}

{{- define "ticketing-system.apiServiceAccountName" -}}
{{- if .Values.api.serviceAccount.create }}
{{- default (include "ticketing-system.apiName" .) .Values.api.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.api.serviceAccount.name }}
{{- end }}
{{- end }}

{{- define "ticketing-system.image" -}}
{{- $root := index . 0 -}}
{{- $image := index . 1 -}}
{{- printf "%s:%s" $image.repository (default $root.Chart.AppVersion $image.tag) -}}
{{- end }}
