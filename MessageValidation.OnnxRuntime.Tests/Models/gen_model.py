# Regenerates max_score.onnx: score = max(features). Used as a stand-in anomaly model in tests.
# pip install onnx ; python gen_model.py
import onnx
from onnx import helper, TensorProto

node = helper.make_node("ReduceMax", ["features"], ["score"], keepdims=0)
graph = helper.make_graph(
    [node], "max_score",
    [helper.make_tensor_value_info("features", TensorProto.FLOAT, [1, "n"])],
    [helper.make_tensor_value_info("score", TensorProto.FLOAT, [])])
model = helper.make_model(graph, opset_imports=[helper.make_opsetid("", 13)], ir_version=7)
onnx.checker.check_model(model)
onnx.save(model, "max_score.onnx")
