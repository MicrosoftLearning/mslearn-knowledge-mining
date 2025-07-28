# Import libraries
import argparse
import glob
import pickle
from pathlib import Path
import matplotlib.pyplot as plt
import mlflow
import numpy as np
import pandas as pd
from sklearn.linear_model import LinearRegression
from sklearn.metrics import mean_squared_error, r2_score
from sklearn.model_selection import train_test_split

# get parameters
parser = argparse.ArgumentParser("train")
parser.add_argument("--training_data", type=str, help="Path to training data")
parser.add_argument("--reg_rate", type=float, default=0.01)
parser.add_argument("--model_output", type=str, help="Path of output model")

args = parser.parse_args()

training_data = args.training_data

# load the prepared data file in the training folder
print("Loading Data...")
data_path = args.training_data
all_files = glob.glob(data_path + "/*.csv")
df = pd.concat((pd.read_csv(f) for f in all_files), sort=False)

# Separate features and labels
X, y = (
    df[
        [
            "symboling",
            "make",
            "fuel-type",
            "aspiration",
            "num-of-doors",
            "body-style",
            "drive-wheels",
            "engine-location",
            "wheel-base",
            "length",
            "width",
            "height",
            "curb-weight",
            "engine-type",
            "num-of-cylinders",
            "engine-size",
            "fuel-system",
            "bore",
            "stroke",
            "compression-ratio",
            "horsepower",
            "peak-rpm",
            "city-mpg",
            "highway-mpg",
            "price",
        ]
    ].values,
    df["predicted_price"].values,
)

# Split data into training set and test set
X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.30, random_state=0
)

# Train a logistic regression model
print('Training a linear regression model...')
model = LinearRegression().fit(X_train, y_train)

# Predict and evaluate
y_pred = model.predict(X_test)
mse = mean_squared_error(y_test, y_pred)
r2 = r2_score(y_test, y_pred)
print("Mean Squared Error:", mse)
print("R^2 Score:", r2)
mlflow.log_metric("MSE", float(mse))
mlflow.log_metric("R2", float(r2))

# Plot predictions vs actuals
plt.figure(figsize=(8, 6))
plt.scatter(y_test, y_pred, alpha=0.7)
plt.xlabel("Actual Price", fontsize=14)
plt.ylabel("Predicted Price", fontsize=14)
plt.title("Actual vs Predicted Price", fontsize=16)
plt.plot([y_test.min(), y_test.max()], [y_test.min(), y_test.max()], 'r--')
plt.savefig("ActualVsPredicted.png")
mlflow.log_artifact("ActualVsPredicted.png")

# Output the model and test data
pickle.dump(model, open((Path(args.model_output) / "model.sav"), "wb"))