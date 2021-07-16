#!/bin/bash
set -euo pipefail

export ROOT=../../..;source $ROOT/scripts/base.sh

${ROOT}/scripts/k8sctx.sh

export ENV=cloud

source ./genfiles.sh

source $ROOT/scripts/login.sh

echo "ACR LOGIN"
az acr login --name ${AZURE_CONTAINER_REGISTRY_SERVER}

echo "DEPLOYING INGRESS"
kubectl apply -f https://aka.ms/tye/ingress/deploy

kubectl delete -A ValidatingWebhookConfiguration ingress-nginx-admission

echo "DEPLOYING APPLICATION"
tye deploy -v Debug --tags $ENV

${ROOT}/scripts/func.sh
${ROOT}/scripts/ingressip.sh