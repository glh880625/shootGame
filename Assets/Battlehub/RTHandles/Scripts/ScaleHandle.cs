﻿using UnityEngine;

using Battlehub.RTCommon;
namespace Battlehub.RTHandles
{
    [DefaultExecutionOrder(2)]
    public class ScaleHandle : BaseHandle
    {
        public float GridSize = 0.1f;
        private Vector3 m_prevPoint;
        private Matrix4x4 m_matrix;
        private Matrix4x4 m_inverse;

        private Vector3 m_roundedScale;
        private Vector3 m_scale;
        private Vector3[] m_refScales;
        private float m_screenScale;

        public override RuntimeTool Tool
        {
            get { return RuntimeTool.Scale; }
        }

        protected override float CurrentGridUnitSize
        {
            get { return GridSize; }
        }

        protected override void AwakeOverride()
        {
            base.AwakeOverride();

            m_scale = Vector3.one;
            m_roundedScale = m_scale;
        }

        protected override void UpdateOverride()
        {
            base.UpdateOverride();
            if (Editor.Tools.IsViewing)
            {
                SelectedAxis = RuntimeHandleAxis.None;
                return;
            }
            if (!IsWindowActive || !Window.IsPointerOver)
            {
                return;
            }
            if (HightlightOnHover && !IsDragging && !IsPointerDown)
            {
                SelectedAxis = Hit();
            }
        }

        private RuntimeHandleAxis Hit()
        {
            m_screenScale = RuntimeHandlesComponent.GetScreenScale(transform.position, Window.Camera) * Appearance.HandleScale;
            m_matrix = Matrix4x4.TRS(transform.position, Rotation, Appearance.InvertZAxis ? new Vector3(1, 1, -1) : Vector3.one);
            m_inverse = m_matrix.inverse;

            if (Model != null)
            {
                return Model.HitTest(Window.Pointer);
            }

            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, Rotation, new Vector3(m_screenScale, m_screenScale, m_screenScale));

            if (HitCenter())
            {
                return RuntimeHandleAxis.Free;
            }
            float distToYAxis;
            float distToZAxis;
            float distToXAxis;
            bool hit = HitAxis(Vector3.up, matrix, out distToYAxis);
            hit |= HitAxis(Appearance.Forward, matrix, out distToZAxis);
            hit |= HitAxis(Vector3.right, matrix, out distToXAxis);

            if (hit)
            {
                if (distToYAxis <= distToZAxis && distToYAxis <= distToXAxis)
                {
                    return RuntimeHandleAxis.Y;
                }
                else if (distToXAxis <= distToYAxis && distToXAxis <= distToZAxis)
                {
                    return RuntimeHandleAxis.X;
                }
                else
                {
                    return RuntimeHandleAxis.Z;
                }
            }

            return RuntimeHandleAxis.None;
        }
        GameObject temporary_Parent;
        protected override bool OnBeginDrag()
        {
            if (!base.OnBeginDrag())
            {
                return false;
            }

            SelectedAxis = Hit();

            if (SelectedAxis == RuntimeHandleAxis.Free)
            {
                DragPlane = GetDragPlane(Vector3.zero);
            }
            else if (SelectedAxis == RuntimeHandleAxis.None)
            {
                return false;
            }

            m_refScales = new Vector3[ActiveTargets.Length];
            for (int i = 0; i < m_refScales.Length; ++i)
            {
                Quaternion rotation = Editor.Tools.PivotRotation == RuntimePivotRotation.Global ? ActiveTargets[i].rotation : Quaternion.identity;
                m_refScales[i] = rotation * ActiveTargets[i].localScale;
            }

            Vector3 axis = Vector3.zero;
            switch (SelectedAxis)
            {
                case RuntimeHandleAxis.X:
                    axis = Vector3.right;
                    break;
                case RuntimeHandleAxis.Y:
                    axis = Vector3.up;
                    break;
                case RuntimeHandleAxis.Z:
                    axis = Vector3.forward;
                    break;
            }

            DragPlane = GetDragPlane(axis);
            bool result = GetPointOnDragPlane(Window.Pointer, out m_prevPoint);
            if (!result)
            {
                SelectedAxis = RuntimeHandleAxis.None;
            }
           
            return result;
        }
        int ItemListNum;
        protected override void OnDrag()
        {
            base.OnDrag();

            Vector3 point;
            if (GetPointOnDragPlane(Window.Pointer, out point))
            {
                Vector3 offset = m_inverse.MultiplyVector((point - m_prevPoint) / m_screenScale);
                float mag = offset.magnitude;
                if (SelectedAxis == RuntimeHandleAxis.X)
                {
                    offset.y = offset.z = 0.0f;

                    if (LockObject == null || !LockObject.ScaleX)
                    {
                        m_scale.x += Mathf.Sign(offset.x) * mag;
                    }
                }
                else if (SelectedAxis == RuntimeHandleAxis.Y)
                {
                    offset.x = offset.z = 0.0f;
                    if (LockObject == null || !LockObject.ScaleY)
                    {
                        m_scale.y += Mathf.Sign(offset.y) * mag;
                    }
                }
                else if (SelectedAxis == RuntimeHandleAxis.Z)
                {
                    offset.x = offset.y = 0.0f;
                    if (LockObject == null || !LockObject.ScaleZ)
                    {
                        m_scale.z += Mathf.Sign(offset.z) * mag;
                    }
                }
                if (SelectedAxis == RuntimeHandleAxis.Free)
                {
                    float sign = Mathf.Sign(offset.x + offset.y);

                    if (LockObject != null)
                    {
                        if (!LockObject.ScaleX)
                        {
                            m_scale.x += sign * mag;
                        }

                        if (!LockObject.ScaleY)
                        {
                            m_scale.y += sign * mag;
                        }

                        if (!LockObject.ScaleZ)
                        {
                            m_scale.z += sign * mag;
                        }
                    }
                    else
                    {
                        m_scale.x += sign * mag;
                        m_scale.y += sign * mag;
                        m_scale.z += sign * mag;

                    }
                }

                m_roundedScale = m_scale;

                if (EffectiveGridUnitSize > 0.01)
                {
                    m_roundedScale.x = Mathf.RoundToInt(m_roundedScale.x / EffectiveGridUnitSize) * EffectiveGridUnitSize;

                    m_roundedScale.y = Mathf.RoundToInt(m_roundedScale.y / EffectiveGridUnitSize) * EffectiveGridUnitSize;

                    m_roundedScale.z = Mathf.RoundToInt(m_roundedScale.z / EffectiveGridUnitSize) * EffectiveGridUnitSize;

                }

                if (Model != null)
                {
                    Model.SetScale(m_roundedScale);
                }

                for (int i = 0; i < m_refScales.Length; ++i)
                {
                    Quaternion rotation = Editor.Tools.PivotRotation == RuntimePivotRotation.Global ? Targets[i].rotation : Quaternion.identity;
                    Vector3 newvector = new Vector3((m_refScales[i].x + (m_roundedScale.x == 1 ? 0 : m_roundedScale.x)) < 1 ? 1 : m_refScales[i].x + (m_roundedScale.x == 1 ? 0 : m_roundedScale.x),
                                                    (m_refScales[i].y),
                                                    (m_refScales[i].z + (m_roundedScale.z == 1 ? 0 : m_roundedScale.z)) < 1 ? 1 : m_refScales[i].z + (m_roundedScale.z == 1 ? 0 : m_roundedScale.z));
                    ActiveTargets[i].localScale = Quaternion.Inverse(rotation) * newvector;
                }

                m_prevPoint = point;
            }

            


            Debug.Log("拖动");
        }

        protected override void OnDrop()
        {
            base.OnDrop();

            m_scale = Vector3.one;
            m_roundedScale = m_scale;
            if (Model != null)
            {
                Model.SetScale(m_roundedScale);
            }
            if (Target != null && (Target.gameObject.layer == LayerMask.NameToLayer("ShootPos") || Target.gameObject.layer == LayerMask.NameToLayer("Item")))
            {
                ShootingItem item = ShootGameEditor._Instance.getActiveItem(Target.gameObject);
                for (int i = 0; i < ShootGameEditor._Instance.GetEditorArea().m_ShootingItem.Count; i++)
                {
                    if (item.Prefab == ShootGameEditor._Instance.GetEditorArea().m_ShootingItem[i].Prefab)
                    {
                        ItemListNum = i;
                        break;
                    }
                }

                General newGeneral = item.m_General;
                newGeneral.scale = Target.localScale;
                item.m_General = newGeneral;

                ShootGameEditor._Instance.GetEditorArea().m_ShootingItem[ItemListNum] = item;
            }
            if (Target != null && Target.gameObject.layer == LayerMask.NameToLayer("Area"))
            {
                General newGeneral = ShootGameEditor._Instance.GetActiveArea(Target.gameObject).m_General;
                newGeneral.scale = Target.localScale;
                ShootGameEditor._Instance.GetActiveArea(Target.gameObject).m_General = newGeneral;
            }
        }

        protected override void DrawOverride(Camera camera)
        {
            Appearance.DoScaleHandle(camera, m_roundedScale, Target.position, Rotation, SelectedAxis, LockObject);
        }
    }
}